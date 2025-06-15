using Zakup.Entities;

namespace app.Services;

using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google;
using Microsoft.EntityFrameworkCore;
using Zakup.EntityFramework;

public class InternalSheetsService
{
    private readonly SheetsService _sheetsService;
    private readonly DriveService _driveService;
    private readonly ApplicationDbContext _context;

    public InternalSheetsService(
        SheetsService sheetsService, 
        DriveService driveService,
        ApplicationDbContext context)
    {
        _sheetsService = sheetsService;
        _driveService = driveService;
        _context = context;
    }

    public async Task<string> UploadFile(Stream stream)
    {
        var fileMetadata = new Google.Apis.Drive.v3.Data.File()
        {
            Name = "image.jpg",
            MimeType = "image/jpeg"
        };
            var request = _driveService.Files.Create(fileMetadata, stream, "image/jpeg");
            request.Fields = "id";
            var file = await request.UploadAsync();

            if (file.Status == Google.Apis.Upload.UploadStatus.Failed)
            {
                throw new Exception($"Error uploading file: {file.Exception.Message}");
            }

            Console.WriteLine($"File ID: {request.ResponseBody.Id}");
        
            // Set permissions for public access
            await DriveShareFile(request.ResponseBody.Id);
            return request.ResponseBody.Id;
        
    }
    
    public async Task ClearAndUpdateInviteLinks()
    {
        var sheets = await _context.ChannelSheets
            .Include(s => s.SpreadSheet)
            .Include(s => s.Channel)
            .ToListAsync();

        foreach (var sheet in sheets)
        {
            if (string.IsNullOrEmpty(sheet.SpreadSheetId))
            {
                Console.WriteLine($"Spreadsheet ID is null or empty for channel {sheet.ChannelId}.");
                continue;
            }

            // Очистка 13-го столбца, начиная со 2-й строки
            var clearRequest = new Request
            {
                UpdateCells = new UpdateCellsRequest
                {
                    Range = new GridRange
                    {
                        SheetId = sheet.Id,
                        StartColumnIndex = 12, // Колонка M (начиная с 0)
                        EndColumnIndex = 13,
                        StartRowIndex = 1, // Начиная со второй строки
                    },
                    Fields = "userEnteredValue"
                }
            };

            // Сбор пригласительных ссылок для заполнения
            var inviteLinks = await _context.TelegramZakups
                .Where(z => z.ChannelId == sheet.ChannelId && !string.IsNullOrEmpty(z.InviteLink))
                .OrderBy(z => z.CreatedUtc) // Сортировка по дате создания
                .Select(z => new { z.InviteLink })
                .Distinct()
                .ToListAsync();

            var updateRequests = inviteLinks.Select((link, index) => new Request
            {
                UpdateCells = new UpdateCellsRequest
                {
                    Start = new GridCoordinate
                    {
                        SheetId = sheet.Id,
                        RowIndex = 1 + index, // Игнорируем шапку
                        ColumnIndex = 12
                    },
                    Rows = new List<RowData>
                    {
                        new RowData
                        {
                            Values = new List<CellData>
                            {
                                new CellData
                                {
                                    UserEnteredValue = new ExtendedValue { StringValue = link.InviteLink }
                                }
                            }
                        }
                    },
                    Fields = "userEnteredValue"
                }
            }).ToList();

            var batchUpdateRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request> { clearRequest }.Concat(updateRequests).ToList()
            };

            try
            {
                await _sheetsService.Spreadsheets.BatchUpdate(batchUpdateRequest, sheet.SpreadSheetId).ExecuteAsync();
                Console.WriteLine($"Updated invite links for sheet {sheet.Id} in spreadsheet {sheet.SpreadSheetId}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update invite links for sheet {sheet.Id}: {ex.Message}");
            }
        }
    }


    public async Task RenameHeaderColumnsAsync()
    {
        // 1) Получаем все листы (ChannelSheets) вместе с родительскими SpreadSheets
        //    Если у вас много одинаковых SpreadSheetId, мы сгруппируем запросы по SpreadSheetId,
        //    чтобы не делать лишних запросов.
        var allSheets = await _context.ChannelSheets
            .Include(s => s.SpreadSheet)
            .ToListAsync();

        // Группируем по SpreadSheetId
        var sheetsBySpreadsheet = allSheets
            .Where(cs => !string.IsNullOrEmpty(cs.SpreadSheetId)) // отсекаем пустые
            .GroupBy(cs => cs.SpreadSheetId)
            .ToList();

        // 2) Проходим по каждой группе (одна Google-таблица)
        foreach (var group in sheetsBySpreadsheet)
        {
            var spreadsheetId = group.Key!; // точно не null из фильтра выше
            var channelSheets = group.ToList();

            Console.WriteLine($"Обрабатываем таблицу: {spreadsheetId}");

            // Скачиваем Spreadsheet целиком с данными, 
            // чтобы получить контент первой строки каждого листа
            var getRequest = _sheetsService.Spreadsheets.Get(spreadsheetId);
            getRequest.IncludeGridData = true;  // нужно включить данные
            Spreadsheet spreadsheet;
            try
            {
                spreadsheet = await getRequest.ExecuteAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запросе Spreadsheet {spreadsheetId}: {ex.Message}");
                continue;
            }

            // Мапим sheetId -> Sheet (Google)
            var googleSheetsDict = spreadsheet.Sheets
                .Where(s => s.Properties != null)
                .ToDictionary(
                    s => s.Properties.SheetId,
                    s => s
                );

            // Собираем все запросы (Request) в один общий список, потом сделаем один BatchUpdate
            var allRequests = new List<Request>();

            // 3) Идём по листам (ChannelSheets), которые относятся к данной Google-таблице
            foreach (var chSheet in channelSheets)
            {
                var sheetId = chSheet.Id; // числовой ID листа
                if (!googleSheetsDict.TryGetValue(sheetId, out var googleSheet))
                {
                    // Лист был удалён/переименован и т.д.
                    Console.WriteLine($" В таблице {spreadsheetId} не найден sheetId={sheetId}");
                    continue;
                }

                // Берем данные (GridData) из googleSheet
                var sheetData = googleSheet.Data?.FirstOrDefault();
                if (sheetData?.RowData == null || !sheetData.RowData.Any())
                {
                    Console.WriteLine($" Лист {sheetId} пуст, пропускаем.");
                    continue;
                }

                // Первая строка шапки
                var headerRow = sheetData.RowData.FirstOrDefault();
                if (headerRow?.Values == null || !headerRow.Values.Any())
                {
                    Console.WriteLine($" Лист {sheetId} (шапка пустая) пропускаем.");
                    continue;
                }

                var headerCells = headerRow.Values; // IList<CellData>

                // Нам нужно искать два старых названия:
                // 1) "Цена за подписчика(оставшихся)" → "Цена за подписчика(оставшегося)"
                // 2) "Отписываемость первые 48ч, в % от отписавшихся" → "Отписываемость первые 48ч(% от отписавшихся)"
                // Соберём словарь "старое -> новое"
                var renameMap = new Dictionary<string, string>()
                {
                    { "Цена за подписчика(оставшихся)", "Цена за подписчика(оставшегося)" },
                    { "Отписываемость первые 48ч, в % от отписавшихся", "Отписываемость первые 48ч(% от отписавшихся)" },
                };

                // Пройдёмся по ячейкам шапки и ищем совпадения
                for (int colIndex = 0; colIndex < headerCells.Count; colIndex++)
                {
                    var cell = headerCells[colIndex];
                    var currentValue = cell?.FormattedValue ?? ""; // что отображается в ячейке
                    if (renameMap.TryGetValue(currentValue, out var newValue))
                    {
                        Console.WriteLine($"  Лист {sheetId}: колонка '{currentValue}' переименуем в '{newValue}'");

                        // Формируем запрос на UpdateCells для одной ячейки (row=0, col=colIndex)
                        var updateReq = new Request
                        {
                            UpdateCells = new UpdateCellsRequest
                            {
                                Range = new GridRange
                                {
                                    SheetId = sheetId,
                                    StartRowIndex = 0,
                                    EndRowIndex = 1,        // только первую строку
                                    StartColumnIndex = colIndex,
                                    EndColumnIndex = colIndex + 1
                                },
                                Rows = new List<RowData>
                                {
                                    new RowData
                                    {
                                        Values = new List<CellData>
                                        {
                                            new CellData
                                            {
                                                UserEnteredValue = new ExtendedValue
                                                {
                                                    StringValue = newValue
                                                }
                                            }
                                        }
                                    }
                                },
                                Fields = "userEnteredValue"
                            }
                        };
                        allRequests.Add(updateReq);
                    }
                }
            }

            // 4) Если накопились запросы — выполняем batchUpdate
            if (allRequests.Any())
            {
                var batchUpdate = new BatchUpdateSpreadsheetRequest
                {
                    Requests = allRequests
                };

                try
                {
                    await _sheetsService.Spreadsheets.BatchUpdate(batchUpdate, spreadsheetId).ExecuteAsync();
                    Console.WriteLine($"В таблице {spreadsheetId} успешно переименованы нужные колонки.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка batchUpdate для {spreadsheetId}: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Нет колонок для переименования в таблице {spreadsheetId}.");
            }
        }

        Console.WriteLine("RenameHeaderColumnsAsync: Done.");
    }


    public async Task FixSheetColumns()
    {
        var sheets = await _context.ChannelSheets.ToListAsync();

        foreach (var sheet in sheets)
        {
            Console.WriteLine($"Checking sheet: SpreadsheetId={sheet.SpreadSheetId}, SheetId={sheet.Id}");

            try
            {
                // 1) Получаем Spreadsheet c данными (IncludeGridData = true),
                //    чтобы видеть, какие значения в шапке (RowData)
                var getRequest = _sheetsService.Spreadsheets.Get(sheet.SpreadSheetId);
                getRequest.IncludeGridData = true;
                var spreadsheet = await getRequest.ExecuteAsync();

                var targetSheet = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.SheetId == sheet.Id);
                if (targetSheet == null)
                {
                    Console.WriteLine("Sheet not found with the given SheetId.");
                    continue;
                }

                // Берем Data из нужного листа:
                var sheetData = targetSheet.Data?.FirstOrDefault();
                if (sheetData?.RowData == null || !sheetData.RowData.Any())
                {
                    Console.WriteLine("No row data found in sheet.");
                    continue;
                }

                // Первая строка (шапка)
                var headerRow = sheetData.RowData.FirstOrDefault();
                if (headerRow?.Values == null)
                {
                    Console.WriteLine("Header row is empty.");
                    continue;
                }

                // Массив ячеек первой строки (шапка)
                var headerCells = headerRow.Values;

                // Сформируем список запросов
                var requests = new List<Request>();

                /********************************************************************
                * 1) Переименовать столбец E (index=4),
                *    если в нем сейчас "Подписчиков 7+ дней(% от всего вступивших)".
                ********************************************************************/
                if (headerCells.Count > 4)
                {
                    var cellE = headerCells[4]; // 0-based => E
                    var currentEValue = cellE?.FormattedValue ?? "";
                    if (currentEValue == "Подписчиков 7+ дней(% от всего вступивших)")
                    {
                        // Переименовываем в "Креатив"
                        requests.Add(new Request
                        {
                            UpdateCells = new UpdateCellsRequest
                            {
                                Range = new GridRange
                                {
                                    SheetId = sheet.Id,
                                    StartRowIndex = 0,
                                    EndRowIndex = 1,       // Только первая строка
                                    StartColumnIndex = 4,  // E
                                    EndColumnIndex = 5
                                },
                                Rows = new List<RowData>
                                {
                                    new RowData
                                    {
                                        Values = new List<CellData>
                                        {
                                            new CellData
                                            {
                                                UserEnteredValue = new ExtendedValue { StringValue = "Креатив" }
                                            }
                                        }
                                    }
                                },
                                Fields = "userEnteredValue"
                            }
                        });
                        Console.WriteLine("Will rename column E back to 'Креатив'.");
                    }
                }

                /********************************************************************
                * 2) Удалить столбец 8 (I) при условии, что в шапке (row=0)
                *    там пустая ячейка (или вообще нет ячейки).
                ********************************************************************/
                bool canDeleteCol8 = false;
                if (headerCells.Count > 8) // значит столбец 8 хотя бы есть
                {
                    var cell8 = headerCells[8];
                    var cell8Value = cell8?.FormattedValue ?? "";
                    // Считаем "пустым", если ячейка null или пустая строка
                    if (string.IsNullOrWhiteSpace(cell8Value))
                    {
                        canDeleteCol8 = true;
                    }
                }

                if (canDeleteCol8)
                {
                    requests.Add(new Request
                    {
                        DeleteDimension = new DeleteDimensionRequest
                        {
                            Range = new DimensionRange
                            {
                                SheetId = sheet.Id,
                                Dimension = "COLUMNS",
                                StartIndex = 8,
                                EndIndex = 9
                            }
                        }
                    });
                    Console.WriteLine("Will remove the empty column at index 8 (I).");
                }

                /********************************************************************
                * 3) Добавить новую колонку между L(11) и M(12), то есть на index=12
                *    и назвать ее "Подписчиков 7+ дней(% от всего вступивших)".
                ********************************************************************/

                // Вставка новой колонки (между L и M)
                requests.Add(new Request
                {
                    InsertDimension = new InsertDimensionRequest
                    {
                        Range = new DimensionRange
                        {
                            SheetId = sheet.Id,
                            Dimension = "COLUMNS",
                            StartIndex = 12,
                            EndIndex = 13
                        }
                    }
                });

                // Обновляем верхнюю ячейку (row=0, col=12)
                requests.Add(new Request
                {
                    UpdateCells = new UpdateCellsRequest
                    {
                        Start = new GridCoordinate
                        {
                            SheetId = sheet.Id,
                            RowIndex = 0,
                            ColumnIndex = 12
                        },
                        Rows = new List<RowData>
                        {
                            new RowData
                            {
                                Values = new List<CellData>
                                {
                                    new CellData
                                    {
                                        UserEnteredValue = new ExtendedValue
                                        {
                                            StringValue = "Подписчиков 7+ дней(% от всего вступивших)"
                                        }
                                    }
                                }
                            }
                        },
                        Fields = "userEnteredValue"
                    }
                });
                Console.WriteLine("Will insert new column at index=12 with header 'Подписчиков 7+ дней(% от всего вступивших)'.");

                // Если нет запросов — пропустим
                if (!requests.Any())
                {
                    Console.WriteLine("No changes needed for this sheet.");
                    continue;
                }

                // Отправляем batchUpdate
                var batchUpdateRequest = new BatchUpdateSpreadsheetRequest { Requests = requests };
                await _sheetsService.Spreadsheets.BatchUpdate(batchUpdateRequest, sheet.SpreadSheetId).ExecuteAsync();
                Console.WriteLine("Batch update successfully executed.");
            }
            catch (Google.GoogleApiException ex)
            {
                Console.WriteLine($"Failed to fix columns: {ex.Message}");
            }
        }
    }
    
    public async Task AddColumnToSheets()
    {
        var sheets = await _context.ChannelSheets.ToListAsync();

        foreach (var sheet in sheets)
        {
            Console.WriteLine($"Checking sheet: SpreadsheetId={sheet.SpreadSheetId}, SheetId={sheet.Id}");

            try
            {
                var spreadsheet = await _sheetsService.Spreadsheets.Get(sheet.SpreadSheetId).ExecuteAsync();
                var targetSheet = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.SheetId == sheet.Id);
                if (targetSheet == null)
                {
                    Console.WriteLine("Sheet not found with the given SheetId.");
                    continue;
                }

                // Проверяем, существует ли уже столбец "Креатив"
                bool columnExists = targetSheet.Data != null && targetSheet.Data.Any(d =>
                    d.RowData != null && d.RowData.Any(r =>
                        r.Values != null && r.Values.Any(v =>
                            v.FormattedValue != null && v.FormattedValue == "Подписчиков 7+ дней(% от всего вступивших)")));

                if (columnExists)
                {
                    Console.WriteLine("Column 'Подписчиков 7+ дней(% от всего вступивших)' already exists.");
                    continue;
                }

                // Если столбец не существует, добавляем его
                var requests = new List<Request>
                {
                    new Request
                    {
                        InsertDimension = new InsertDimensionRequest
                        {
                            Range = new DimensionRange
                            {
                                SheetId = sheet.Id,
                                Dimension = "COLUMNS",
                                StartIndex = 8,  // Индекс, где должен быть добавлен столбец
                                EndIndex = 9
                            }
                        }
                    },
                    new Request
                    {
                        UpdateCells = new UpdateCellsRequest
                        {
                            Start = new GridCoordinate
                            {
                                SheetId = sheet.Id,
                                RowIndex = 0,
                                ColumnIndex = 4
                            },
                            Rows = new List<RowData>
                            {
                                new RowData
                                {
                                    Values = new List<CellData>
                                    {
                                        new CellData
                                        {
                                            UserEnteredValue = new ExtendedValue { StringValue = "Подписчиков 7+ дней(% от всего вступивших)" }
                                        }
                                    }
                                }
                            },
                            Fields = "userEnteredValue"
                        }
                    }
                };

                var batchUpdateRequest = new BatchUpdateSpreadsheetRequest { Requests = requests };
                await _sheetsService.Spreadsheets.BatchUpdate(batchUpdateRequest, sheet.SpreadSheetId).ExecuteAsync();
                Console.WriteLine("Column 'Подписчиков 7+ дней(% от всего вступивших)' added successfully.");
            }
            catch (GoogleApiException ex)
            {
                Console.WriteLine($"Failed to check or update sheet: {ex.Message}");
            }
        }
    }


    public async Task CreateSheet(long channelId, string sheetName, string spreadSheetName, long userId)
    {
        if (_context.ChannelSheets.Any(s => s.ChannelId == channelId && s.SpreadSheet.UserId == userId))
        {
            return;
        }
        
        var spreadSheetEntity = await _context.SpreadSheets.FirstOrDefaultAsync(s => s.UserId == userId);
        var addSheetRequest = new AddSheetRequest
        {
            Properties = new SheetProperties
            {
                Title = sheetName
            }
        };
        var batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request>()
            };
        batchUpdateSpreadsheetRequest.Requests.Add(new Request
        {
            AddSheet = addSheetRequest
        });
        
        if (spreadSheetEntity is null)
        {
            var spreadSheetRequest = _sheetsService.Spreadsheets.Create(new Spreadsheet()
            {
                Properties = new SpreadsheetProperties()
                {
                    Title = $"zakup_{spreadSheetName}"
                }
            });
            
            var spreadSheet = await spreadSheetRequest.ExecuteAsync();
            await DriveShareFile(spreadSheet.SpreadsheetId);
            spreadSheetEntity = new UserSpreadSheet
            {
                Id = spreadSheet.SpreadsheetId,
                UserId = userId,
            };
        }
        var sheetRequest =
            _sheetsService.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, spreadSheetEntity.Id);
        var sheet = await sheetRequest.ExecuteAsync();
        
        var sheetEntity = new ChannelSheet
        {
            Id = sheet.Replies.First().AddSheet!.Properties!.SheetId!.Value,
            ChannelId = channelId,
            SpreadSheet = spreadSheetEntity
        };
        
        await _context.AddAsync(sheetEntity);
        await _context.SaveChangesAsync();
		
		await AppendRow(userId, channelId, new List<object> { 
            "Дата создания закупа",
            "Платформа",
            "Цена",
            "Админ",
            "Креатив",
            "Реквизиты",
            "Оплачено",
            "Сейчас в канале",
            "Покинуло канал",
            "Цена за подписчика(оставшегося)",
            "Отписываемость первые 48ч(% от отписавшихся)",
            "Подписчиков 7+ дней(% от всего вступивших)",
            "Премиум пользователей",
            "Пригласительная ссылка (не удалять)",
            "Клиентов по ссылке",
            "Комментирует из подписавшихся(%)" 
        });
    }

    public async Task AppendRow(long userId, long channelId, List<object> cells)
    {
        var sheetEntity = await _context.ChannelSheets
            .Include(s => s.SpreadSheet)
            .Include(s => s.Channel)
            .Where(s => s.SpreadSheet.UserId == userId)
            .FirstOrDefaultAsync(s => s.ChannelId == channelId);

        var channel = await _context.Channels.FirstOrDefaultAsync(q => q.Id == channelId);
        
        if (channel is null)
        {
            return;
        }
        
        if (sheetEntity is null)
        {
           await CreateSheet(channelId,channel.Title,"stat",userId);
        }
        
        sheetEntity = await _context.ChannelSheets
            .Include(s => s.SpreadSheet)
            .Include(s => s.Channel)
            .Where(s => s.SpreadSheet.UserId == userId)
            .FirstOrDefaultAsync(s => s.ChannelId == channelId);

        if (sheetEntity is null) 
        {
            return;
        }
        
        var data = new ValueRange()
        {
            Values = new List<IList<object>> { cells }
        };
        var r =  _sheetsService.Spreadsheets.Values.Append(data, sheetEntity!.SpreadSheetId, sheetEntity.Channel.Title);
        
        r.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
        await r.ExecuteAsync();
    }
    
    public async Task AppendRowByHeaders(long userId, long channelId, Dictionary<string, object> dataByColumnName)
    {
        // 1) Ищем нужный лист (Sheet) в БД
        var sheetEntity = await _context.ChannelSheets
            .Include(s => s.SpreadSheet)
            .Include(s => s.Channel)
            .Where(s => s.SpreadSheet.UserId == userId)
            .FirstOrDefaultAsync(s => s.ChannelId == channelId);

        if (sheetEntity == null)
        {
            Console.WriteLine($"SheetEntity of user {userId} not found for this user/channel(channelId = {channelId}). Possibly no spreadsheet exists yet?");
            return;
        }

        // 2) Читаем первую строку (A1:Z1), чтобы узнать названия столбцов
        var sheetTitle = sheetEntity.Channel.Title; // Имя листа = название канала
        var headerRange = $"{sheetTitle}!A1:Z1";

        var getRequest = _sheetsService.Spreadsheets.Values.Get(sheetEntity.SpreadSheetId, headerRange);
        ValueRange? headerResponse;
        try
        {
            headerResponse = await getRequest.ExecuteAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading header row: {ex.Message}");
            return;
        }

        if (headerResponse.Values == null || headerResponse.Values.Count == 0)
        {
            Console.WriteLine("Sheet seems empty (no header row found).");
            // В таком случае можно либо создавать заголовок, либо бросать исключение
            return;
        }

        // Предполагаем, что headerResponse.Values[0] — это список названий столбцов
        var headerRow = headerResponse.Values[0];

        // 3) Определяем максимальное число столбцов (по факту, можно взять headerRow.Count или 26)
        //    Чтобы если у нас 15 столбцов в шапке, можно сформировать массив на 15
        int maxColumnCount = headerRow.Count;

        // Найдём индексы для всех требуемых колонок
        // Пример: dataByColumnName["Цена"] => значение 100
        // Нужно понять, в каком индексе (0-based) "Цена" находится в шапке
        var columnIndexDict = new Dictionary<string, int>();
        for (int i = 0; i < headerRow.Count; i++)
        {
            var colName = headerRow[i]?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(colName))
            {
                // Если в словаре dataByColumnName есть такой ключ, запомним индекс
                if (dataByColumnName.ContainsKey(colName))
                {
                    columnIndexDict[colName] = i;
                }
            }
        }

        // 4) Строим новую строку, где нужные столбцы заполнены, а остальные — пусты
        var newRow = new object[maxColumnCount];
        for (int i = 0; i < maxColumnCount; i++)
        {
            newRow[i] = ""; // по умолчанию пустая строка
        }

        // Заполняем те ячейки, для которых мы нашли индекс
        foreach (var pair in dataByColumnName)
        {
            var colName = pair.Key;
            var value = pair.Value;
            if (columnIndexDict.TryGetValue(colName, out var idx))
            {
                newRow[idx] = value;
            }
            // Если столбец не найден — игнорируем
        }

        // 5) Делаем Append именно одного ряда
        var data = new ValueRange
        {
            Values = new List<IList<object>> { newRow }
        };

        var appendRequest = _sheetsService.Spreadsheets.Values.Append(data, sheetEntity.SpreadSheetId, sheetTitle);
        appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

        try
        {
            await appendRequest.ExecuteAsync();
            Console.WriteLine("Row appended successfully by header names.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error appending row: {ex.Message}");
        }
    }


    public IQueryable<ChannelMember> GetMembersByInviteLink(string inviteLink)
    {
        return _context.ChannelMembers.Where(m => m.InviteLink == inviteLink);
    }

    public async Task UpdateCreativeTitles()
    {
        var sheets = await _context.ChannelSheets
            .Include(s => s.SpreadSheet)
            .Include(s => s.Channel)
            .ToListAsync();

        foreach (var sheet in sheets)
        {
            var range = $"{sheet.Channel.Title}!A2:O"; // Предполагаем, что данные начинаются со 2-й строки
            ValueRange response;

            try
            {
                response = await _sheetsService.Spreadsheets.Values.Get(sheet.SpreadSheet.Id, range).ExecuteAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving data: {ex.Message}");
                continue;
            }

            if (response.Values == null || !response.Values.Any())
            {
                Console.WriteLine("No data available.");
                continue;
            }

            var updates = new List<ValueRange>();
            for (int i = 0; i < response.Values.Count; i++)
            {
                var row = response.Values[i];
                if (row.Count < 13 || row[12] == null) // Предполагаем, что инвайт линк находится в столбце M
                {
                    Console.WriteLine($"Row {i + 2} has insufficient data.");
                    continue;
                }

                var inviteLink = row[12].ToString();
                var zakup = await _context.TelegramZakups
                    .Include(z => z.AdPost)
                    .FirstOrDefaultAsync(z => z.InviteLink == inviteLink);

                if (zakup?.AdPost?.Title != null && (row.Count <= 4 || string.IsNullOrEmpty(row[4]?.ToString())))
                {
                    // Update the fifth column (index 4) if it's empty
                    var updateValues = new List<object>(row);
                    while (updateValues.Count <= 4) updateValues.Add(null); // Ensure the list is long enough
                    updateValues[4] = zakup.AdPost.Title; // Set the creative title

                    var updateRange = $"{sheet.Channel.Title}!A{2 + i}:O{2 + i}";
                    updates.Add(new ValueRange { Range = updateRange, Values = new List<IList<object>> { updateValues } });
                }
            }

            // Perform the batch update if there are any updates
            if (updates.Count > 0)
            {
                var requestBody = new BatchUpdateValuesRequest
                {
                    ValueInputOption = "USER_ENTERED",
                    Data = updates
                };
                var batchUpdateRequest = _sheetsService.Spreadsheets.Values.BatchUpdate(requestBody, sheet.SpreadSheet.Id);
                await batchUpdateRequest.ExecuteAsync();
                Console.WriteLine($"Updated creative titles in '{sheet.Channel.Title}'");
            }
        }
    }


    public async Task UpdateStatistic(long userId)
    {
        

        // 1) Собираем все листы (ChannelSheets) пользователя
        var sheets = await _context.ChannelSheets
            .Include(s => s.SpreadSheet)
            .Include(s => s.Channel)
            .Where(s => s.SpreadSheet.UserId == userId)
            .ToListAsync();

        if (!sheets.Any())
        {
            Console.WriteLine($"No sheets found for userId={userId}.");
            return;
        }

        // Берём SpreadsheetId (если у пользователя только одна таблица)
        var spreadsheetId = sheets.First().SpreadSheetId;
        if (string.IsNullOrEmpty(spreadsheetId))
        {
            Console.WriteLine("Spreadsheet ID is empty. Cannot proceed.");
            return;
        }

        // 2) Считываем метаданные о таблице, чтобы узнать актуальные названия листов
        Spreadsheet spreadsheet;
        try
        {
            spreadsheet = await _sheetsService.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cannot fetch spreadsheet {spreadsheetId}: {ex.Message}");
            return;
        }

        // Собираем набор существующих названий листов
        var googleSheetTitles = spreadsheet.Sheets
            .Select(s => s.Properties.Title)
            .Where(t => !string.IsNullOrEmpty(t))
            .ToHashSet();

        // 3) Формируем два параллельных списка: validSheets и validRanges
        //    — только для тех ChannelSheets, чьи названия листов действительно есть в Google
        var validSheets = new List<ChannelSheet>();
        var validRanges = new List<string>();

        foreach (var sheetEntity in sheets)
        {
            var sheetTitle = sheetEntity.Channel.Title;
            if (!googleSheetTitles.Contains(sheetTitle))
            {
                Console.WriteLine($"Sheet '{sheetTitle}' was removed or renamed in Google. Skipping...");
                continue;
            }

            validSheets.Add(sheetEntity);
            validRanges.Add($"{sheetTitle}!A1:Z1000");
        }

        if (!validSheets.Any())
        {
            Console.WriteLine("No valid sheets found to update. All were removed or renamed.");
            return;
        }

        // 4) Делаем один общий запрос BatchGet только по validRanges
        var batchGetRequest = _sheetsService.Spreadsheets.Values.BatchGet(spreadsheetId);
        batchGetRequest.Ranges = validRanges;
        BatchGetValuesResponse batchGetResponse;
        try
        {
            batchGetResponse = await batchGetRequest.ExecuteAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"BatchGet failed: {ex.Message}");
            return;
        }

        // 5) Собираем все ссылки (inviteLink) — единая выборка из БД
        var allInviteLinks = new HashSet<string>();

        // Словарь для «range → sheetEntity» можно не делать,
        // так как индексы batchGetResponse.ValueRanges[i] теперь
        // соответствуют validSheets[i].
        for (int i = 0; i < batchGetResponse.ValueRanges.Count; i++)
        {
            var valueRange = batchGetResponse.ValueRanges[i];
            var sheetEntity = validSheets[i];

            // Если на листе вообще ничего нет, пропускаем
            if (valueRange.Values == null || !valueRange.Values.Any())
                continue;

            // Первая строка — заголовки
            var headerRow = valueRange.Values[0];
            int inviteLinkIndex = FindColumnIndex(headerRow, "Пригласительная ссылка (не удалять)");
            if (inviteLinkIndex == -1)
            {
                Console.WriteLine($"Sheet '{sheetEntity.Channel.Title}' does not contain invite-link column. Skipped.");
                continue;
            }

            // Собираем inviteLinks
            for (int rowIndex = 1; rowIndex < valueRange.Values.Count; rowIndex++)
            {
                var row = valueRange.Values[rowIndex];
                if (row.Count <= inviteLinkIndex) 
                    continue;

                var linkObj = row[inviteLinkIndex];
                if (linkObj == null) 
                    continue;

                var inviteLink = linkObj.ToString();
                if (!string.IsNullOrEmpty(inviteLink))
                    allInviteLinks.Add(inviteLink);
            }
        }

        // 6) Грузим статистику из БД для всех inviteLinks
        var statsDictionary = await GatherStatsForInviteLinks(allInviteLinks.ToList());

        // 7) Формируем список обновлений (ValueRange) и потом один BatchUpdate
        var allUpdates = new List<ValueRange>();

        for (int i = 0; i < batchGetResponse.ValueRanges.Count; i++)
        {
            var valueRange = batchGetResponse.ValueRanges[i];
            var sheetEntity = validSheets[i];
            
            if (valueRange.Values == null || valueRange.Values.Count < 1)
                continue;

            var headerRow = valueRange.Values[0];
            int inviteLinkIndex = FindColumnIndex(headerRow, "Пригласительная ссылка (не удалять)");
            if (inviteLinkIndex == -1)
                continue;

            // Карта «название столбца → ключ из stats»
            var columnsMap = new Dictionary<string, string>()
            {
                { "Сейчас в канале", "currentInChannel" },
                { "Покинуло канал", "leftChannel" },
                { "Цена за подписчика(оставшегося)", "pricePerSubscriber" },
                { "Отписываемость первые 48ч(% от отписавшихся)", "leftRateFirst48H" },
                { "Премиум пользователей", "premiumUsers" },
                { "Клиентов по ссылке", "clientCount" },
                { "Комментирует из подписавшихся(%)", "commentersCount" },
                { "Подписчиков 7+ дней(% от всего вступивших)", "spent7DaysPercent" },
            };

            // Ищем индекс «Цена», если нужен
            int priceIndex = FindColumnIndex(headerRow, "Цена");

            // Составляем карту «название столбца → индекс колонки»
            var columnIndexDict = new Dictionary<string, int>();
            foreach (var colName in columnsMap.Keys)
            {
                var idx = FindColumnIndex(headerRow, colName);
                columnIndexDict[colName] = idx; // -1 если отсутствует
            }

            // Идём по строкам (реальные данные)
            for (int rowIndex = 1; rowIndex < valueRange.Values.Count; rowIndex++)
            {
                var row = valueRange.Values[rowIndex];
                if (row.Count <= inviteLinkIndex) 
                    continue;

                var linkObj = row[inviteLinkIndex];
                if (linkObj == null) 
                    continue;

                var inviteLink = linkObj.ToString();
                if (string.IsNullOrEmpty(inviteLink))
                    continue;

                if (!statsDictionary.TryGetValue(inviteLink, out var stats))
                    continue; // нет статистики

                // Считаем «цену»
                decimal price = 0;
                if (priceIndex >= 0 && row.Count > priceIndex)
                    decimal.TryParse(row[priceIndex]?.ToString(), out price);

                var currentInChannel = stats["currentInChannel"];
                var leftTotal = stats["leftTotal"];
                var pricePerSubscriber = (currentInChannel > 0) ? (price / currentInChannel) : 0m;
                var leftRate48H = (leftTotal > 0) 
                    ? (double)stats["leftWithin48Hours"] / leftTotal * 100 
                    : 0;

                var rowValuesForUpdate = new Dictionary<string, object>()
                {
                    { "currentInChannel",   currentInChannel },
                    { "leftChannel",        stats["leftChannel"] },
                    { "pricePerSubscriber", pricePerSubscriber },
                    { "leftRateFirst48H",   leftRate48H },
                    { "premiumUsers",       stats["premiumUsers"] },
                    { "clientCount",        stats["clientCount"] },
                    { "commentersCount",    stats["commentersCount"] },
                    { "spent7DaysPercent",  stats["spent7DaysPercent"] },
                };

                // Формируем ValueRange'ы
                foreach (var (colName, statKey) in columnsMap)
                {
                    int idx = columnIndexDict[colName];
                    if (idx < 0) // столбец удалён
                        continue;

                    var newValue = rowValuesForUpdate[statKey];
                    var realRowNumber = rowIndex + 1; // 1-based
                    var columnLetter = ColumnIndexToLetter(idx);
                    var cellRange = $"{sheetEntity.Channel.Title}!{columnLetter}{realRowNumber}";

                    var singleCellValueRange = new ValueRange
                    {
                        Range = cellRange,
                        Values = new List<IList<object>> { new List<object> { newValue } }
                    };
                    allUpdates.Add(singleCellValueRange);
                }
            }
        }

        if (allUpdates.Any())
        {
            var requestBody = new BatchUpdateValuesRequest
            {
                ValueInputOption = "USER_ENTERED",
                Data = allUpdates
            };

            var batchUpdateRequest = _sheetsService.Spreadsheets.Values.BatchUpdate(requestBody, spreadsheetId);
            await batchUpdateRequest.ExecuteAsync();
            Console.WriteLine("Statistics updated by column names successfully.");
        }
        else
        {
            Console.WriteLine("No updates needed.");
        }
    }

    /// <summary>
    /// Ищет индекс столбца в headerRow по его названию (точное совпадение).
    /// Возвращает -1, если столбец не найден.
    /// </summary>
    private int FindColumnIndex(IList<object> headerRow, string columnName)
    {
        for (int i = 0; i < headerRow.Count; i++)
        {
            if (headerRow[i] != null && headerRow[i].ToString().Trim() == columnName)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Преобразование 0-based индекса в буквенное обозначение столбца (A,B,...Z,AA,AB,...)
    /// </summary>
    private string ColumnIndexToLetter(int columnIndex)
    {
        // Простейшая реализация: перебираем буквы от A до Z, потом AA, AB...
        columnIndex++; // переходим из 0-based в 1-based
        string columnLetter = "";
        while (columnIndex > 0)
        {
            int remainder = (columnIndex - 1) % 26;
            columnLetter = (char)(remainder + 'A') + columnLetter;
            columnIndex = (columnIndex - 1) / 26;
        }
        return columnLetter;
    }

    /// <summary>
    /// Пример групповой выборки статистики по InviteLink.
    /// Возвращаем словарь: inviteLink
    /// </summary>
    private async Task<Dictionary<string, Dictionary<string, int>>> GatherStatsForInviteLinks(List<string> inviteLinks)
    {
        var result = new Dictionary<string, Dictionary<string, int>>();
        if (!inviteLinks.Any()) return result;
        
        var now = DateTime.UtcNow;

        // Считаем TelegramChannel_Members сгруппированно
        var memberStats = await _context.ChannelMembers
            .Where(m => inviteLinks.Contains(m.InviteLink))
            .GroupBy(m => m.InviteLink)
            .Select(g => new
            {
                InviteLink = g.Key,
                CurrentInChannel = g.Count(m => m.LeftUtc == null || m.JoinedUtc > m.LeftUtc),
                LeftChannel = g.Count(m => m.LeftUtc != null && m.JoinedUtc < m.LeftUtc),
                PremiumUsers = g.Count(m => m.IsPremium == true && (m.LeftUtc == null || m.JoinedUtc > m.LeftUtc)),
                LeftWithin48Hours = g.Count(m => m.JoinedUtc.HasValue && m.LeftUtc.HasValue &&
                                        (m.LeftUtc.Value - m.JoinedUtc.Value).TotalHours <= 48),
                TotalMembers = g.Count(),
                Spent7DaysOrMore = g.Count(m =>
                    m.JoinedUtc != null && 
                    m.LeftUtc == null && // Не отписался
                    (now - m.JoinedUtc) >= TimeSpan.FromDays(7) // Пробыл более 7 дней
                ),
                CommentersCount = g.Count(m => m.IsCommenter ?? false)
            })
            .ToListAsync();

        // Считаем клиентов тоже группировано
        var clientCounts = await _context.ZakupClients
            .Where(z => inviteLinks.Contains(z.Zakup.InviteLink))
            .GroupBy(z => z.Zakup.InviteLink)
            .Select(g => new
            {
                InviteLink = g.Key,
                ClientCount = g.Count()
            })
            .ToListAsync();

        // Преобразуем в словарь для быстрого поиска ClientCount
        var clientCountDict = clientCounts.ToDictionary(c => c.InviteLink, c => c.ClientCount);

        // Собираем финальные данные
        foreach (var stat in memberStats)
        {   
            int spent7DaysPct = 0;
            if (stat.TotalMembers > 0)
            {
                var fraction = (stat.Spent7DaysOrMore / (double)stat.TotalMembers) * 100.0;
                spent7DaysPct = (int)Math.Round(fraction);
            }

            var stats = new Dictionary<string, int>
            {
                ["currentInChannel"]   = stat.CurrentInChannel,
                ["leftChannel"]        = stat.LeftChannel,
                ["premiumUsers"]       = stat.PremiumUsers,
                ["leftWithin48Hours"]  = stat.LeftWithin48Hours,
                ["leftTotal"]          = stat.LeftChannel,
                ["commentersCount"]    = stat.TotalMembers > 0 
                    ? (int)Math.Round((stat.CommentersCount / (double)stat.TotalMembers) * 100)
                    : 0,
                ["clientCount"]        = clientCountDict.TryGetValue(stat.InviteLink, out var c) ? c : 0,
                ["spent7DaysPercent"]  = spent7DaysPct
            };

            result[stat.InviteLink] = stats;
        }

        return result;
    }

    private async Task DriveShareFile(string fileId)
    {
        var permission = new Permission()
        {
            Role = "writer",
            Type = "anyone"
        };

        var request = _driveService.Permissions.Create(permission, fileId);
        await request.ExecuteAsync();
    }


    private async Task EnsureTableExists()
    {
        
    }
}
