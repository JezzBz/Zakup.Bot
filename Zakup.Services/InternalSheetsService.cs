using Zakup.Entities;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google;
using Microsoft.EntityFrameworkCore;
using Zakup.EntityFramework;
using Microsoft.Extensions.Logging;

namespace Zakup.Services;

public class InternalSheetsService
{
    private readonly SheetsService _sheetsService;
    private readonly DriveService _driveService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InternalSheetsService> _logger;

    private const string INVITE_LINK_COLUMN = "Пригласительная ссылка (не удалять)";
    private const string CREATIVE_COLUMN = "Креатив";
    private const string PRICE_COLUMN = "Цена";
    private const int DEFAULT_ROW_START = 1;
    private const int MAX_COLUMNS = 26;

    private static readonly Dictionary<string, string> ColumnStatsMap = new()
    {
        { "Сейчас в канале", "currentInChannel" },
        { "Покинуло канал", "leftChannel" },
        { "Цена за подписчика(оставшегося)", "pricePerSubscriber" },
        { "Отписываемость первые 48ч(% от отписавшихся)", "leftRateFirst48H" },
        { "Премиум пользователей", "premiumUsers" },
        { "Клиентов по ссылке", "clientCount" },
        { "Комментирует из подписавшихся(%)", "commentersCount" },
        { "Подписчиков 7+ дней(% от всего вступивших)", "spent7DaysPercent" }
    };

    public InternalSheetsService(
        SheetsService sheetsService,
        DriveService driveService,
        ApplicationDbContext context,
        ILogger<InternalSheetsService> logger)
    {
        _sheetsService = sheetsService;
        _driveService = driveService;
        _context = context;
        _logger = logger;
    }

    public async Task<string> UploadFile(Stream stream)
    {
        var fileMetadata = new Google.Apis.Drive.v3.Data.File
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

        await DriveShareFile(request.ResponseBody.Id);
        return request.ResponseBody.Id;
    }

    public async Task ClearAndUpdateInviteLinks()
    {
        var sheets = await GetChannelSheetsWithIncludes();
        
        foreach (var sheet in sheets)
        {
            if (string.IsNullOrEmpty(sheet.SpreadSheetId))
            {
                _logger.LogWarning("Spreadsheet ID is null or empty for channel {ChannelId}", sheet.ChannelId);
                continue;
            }

            await UpdateInviteLinksForSheet(sheet);
        }
    }
    
    public async Task<bool> CheckIfSheetExists(long userId, long channelId)
    {
        // Проверяем, есть ли запись, связывающая канал, лист и таблицу пользователя
        var sheetExists = await _context.ChannelSheets
            .Include(s => s.SpreadSheet)
            .Include(s => s.Channel)
            .AnyAsync(s => s.ChannelId == channelId && s.SpreadSheet.UserId == userId);
        return sheetExists;
    }

    private async Task UpdateInviteLinksForSheet(ChannelSheet sheet)
    {
        var clearRequest = CreateClearRequest(sheet.Id);
        var inviteLinks = await GetInviteLinksForChannel(sheet.ChannelId);
        var updateRequests = CreateUpdateRequests(sheet.Id, inviteLinks);
        
        var batchUpdateRequest = new BatchUpdateSpreadsheetRequest
        {
            Requests = new List<Request> { clearRequest }.Concat(updateRequests).ToList()
        };

        try
        {
            await _sheetsService.Spreadsheets.BatchUpdate(batchUpdateRequest, sheet.SpreadSheetId).ExecuteAsync();
            _logger.LogInformation("Updated invite links for sheet {SheetId} in spreadsheet {SpreadSheetId}", 
                sheet.Id, sheet.SpreadSheetId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update invite links for sheet {SheetId}", sheet.Id);
        }
    }

    private Request CreateClearRequest(int sheetId) => new()
    {
        UpdateCells = new UpdateCellsRequest
        {
            Range = new GridRange
            {
                SheetId = sheetId,
                StartColumnIndex = 12,
                EndColumnIndex = 13,
                StartRowIndex = DEFAULT_ROW_START
            },
            Fields = "userEnteredValue"
        }
    };

    private async Task<List<string>> GetInviteLinksForChannel(long channelId)
    {
        return await _context.TelegramZakups
            .Where(z => z.ChannelId == channelId && !string.IsNullOrEmpty(z.InviteLink))
            .OrderBy(z => z.CreatedUtc)
            .Select(z => z.InviteLink)
            .Distinct()
            .ToListAsync();
    }

    private List<Request> CreateUpdateRequests(int sheetId, List<string> inviteLinks)
    {
        return inviteLinks.Select((link, index) => new Request
        {
            UpdateCells = new UpdateCellsRequest
            {
                Start = new GridCoordinate
                {
                    SheetId = sheetId,
                    RowIndex = DEFAULT_ROW_START + index,
                    ColumnIndex = 12
                },
                Rows = new List<RowData>
                {
                    new()
                    {
                        Values = new List<CellData>
                        {
                            new() { UserEnteredValue = new ExtendedValue { StringValue = link } }
                        }
                    }
                },
                Fields = "userEnteredValue"
            }
        }).ToList();
    }

    private async Task<List<ChannelSheet>> GetChannelSheetsWithIncludes()
    {
        return await _context.ChannelSheets
            .Include(s => s.SpreadSheet)
            .Include(s => s.Channel)
            .ToListAsync();
    }

    public async Task RenameHeaderColumnsAsync()
    {
        var sheetsBySpreadsheet = await GroupSheetsBySpreadsheet();
        
        foreach (var group in sheetsBySpreadsheet)
        {
            await ProcessSpreadsheetGroup(group);
        }
    }

    private async Task<List<IGrouping<string, ChannelSheet>>> GroupSheetsBySpreadsheet()
    {
        var allSheets = await GetChannelSheetsWithIncludes();
        return allSheets
            .Where(cs => !string.IsNullOrEmpty(cs.SpreadSheetId))
            .GroupBy(cs => cs.SpreadSheetId)
            .ToList();
    }

    private async Task ProcessSpreadsheetGroup(IGrouping<string, ChannelSheet> group)
    {
        var spreadsheetId = group.Key;
        var channelSheets = group.ToList();

        _logger.LogInformation("Processing spreadsheet: {SpreadsheetId}", spreadsheetId);

        var spreadsheet = await GetSpreadsheet(spreadsheetId);
        if (spreadsheet == null) return;

        var googleSheetsDict = CreateGoogleSheetsDictionary(spreadsheet);
        var allRequests = new List<Request>();

        foreach (var chSheet in channelSheets)
        {
            var requests = await ProcessChannelSheet(chSheet, googleSheetsDict);
            allRequests.AddRange(requests);
        }

        await ExecuteBatchUpdate(spreadsheetId, allRequests);
    }

    private async Task<Spreadsheet> GetSpreadsheet(string spreadsheetId)
    {
        try
        {
            var getRequest = _sheetsService.Spreadsheets.Get(spreadsheetId);
            getRequest.IncludeGridData = true;
            return await getRequest.ExecuteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching spreadsheet {SpreadsheetId}", spreadsheetId);
            return null;
        }
    }

    private Dictionary<int, Sheet> CreateGoogleSheetsDictionary(Spreadsheet spreadsheet)
    {
        return spreadsheet.Sheets
            .Where(s => s.Properties != null)
            .ToDictionary(
                s => s.Properties.SheetId ?? 0,
                s => s
            );
    }

    private async Task<List<Request>> ProcessChannelSheet(ChannelSheet chSheet, Dictionary<int, Sheet> googleSheetsDict)
    {
        var requests = new List<Request>();
        
        if (!googleSheetsDict.TryGetValue(chSheet.Id, out var googleSheet))
        {
            _logger.LogWarning("Sheet {SheetId} not found in spreadsheet", chSheet.Id);
            return requests;
        }

        var sheetData = googleSheet.Data?.FirstOrDefault();
        if (sheetData?.RowData == null || !sheetData.RowData.Any())
        {
            _logger.LogWarning("Sheet {SheetId} is empty", chSheet.Id);
            return requests;
        }

        var headerRow = sheetData.RowData.FirstOrDefault();
        if (headerRow?.Values == null || !headerRow.Values.Any())
        {
            _logger.LogWarning("Sheet {SheetId} has empty header", chSheet.Id);
            return requests;
        }

        return CreateRenameRequests(chSheet.Id, headerRow.Values);
    }

    private List<Request> CreateRenameRequests(int sheetId, IList<CellData> headerCells)
    {
        var requests = new List<Request>();
        var renameMap = new Dictionary<string, string>
        {
            { "Цена за подписчика(оставшихся)", "Цена за подписчика(оставшегося)" },
            { "Отписываемость первые 48ч, в % от отписавшихся", "Отписываемость первые 48ч(% от отписавшихся)" }
        };

        for (int colIndex = 0; colIndex < headerCells.Count; colIndex++)
        {
            var cell = headerCells[colIndex];
            var currentValue = cell?.FormattedValue ?? "";
            
            if (renameMap.TryGetValue(currentValue, out var newValue))
            {
                requests.Add(CreateRenameRequest(sheetId, colIndex, newValue));
            }
        }

        return requests;
    }

    private Request CreateRenameRequest(int sheetId, int colIndex, string newValue)
    {
        return new Request
        {
            UpdateCells = new UpdateCellsRequest
            {
                Range = new GridRange
                {
                    SheetId = sheetId,
                    StartRowIndex = 0,
                    EndRowIndex = 1,
                    StartColumnIndex = colIndex,
                    EndColumnIndex = colIndex + 1
                },
                Rows = new List<RowData>
                {
                    new()
                    {
                        Values = new List<CellData>
                        {
                            new() { UserEnteredValue = new ExtendedValue { StringValue = newValue } }
                        }
                    }
                },
                Fields = "userEnteredValue"
            }
        };
    }

    private async Task ExecuteBatchUpdate(string spreadsheetId, List<Request> requests)
    {
        if (!requests.Any())
        {
            _logger.LogInformation("No updates needed for spreadsheet {SpreadsheetId}", spreadsheetId);
            return;
        }

        try
        {
            var batchUpdate = new BatchUpdateSpreadsheetRequest { Requests = requests };
            await _sheetsService.Spreadsheets.BatchUpdate(batchUpdate, spreadsheetId).ExecuteAsync();
            _logger.LogInformation("Successfully updated columns in spreadsheet {SpreadsheetId}", spreadsheetId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute batch update for spreadsheet {SpreadsheetId}", spreadsheetId);
        }
    }

    public async Task<UserSpreadSheet?> GetUserSheet(long userId)
    {
        return await _context.SpreadSheets.FirstOrDefaultAsync(s => s.UserId == userId);
    }
    
    public async Task FixSheetColumns()
    {
        var sheets = await _context.ChannelSheets.ToListAsync();

        foreach (var sheet in sheets)
        {
            _logger.LogInformation("Checking sheet: SpreadsheetId={SpreadsheetId}, SheetId={SheetId}", 
                sheet.SpreadSheetId, sheet.Id);

            try
            {
                await ProcessSheetColumns(sheet);
            }
            catch (GoogleApiException ex)
            {
                _logger.LogError(ex, "Failed to fix columns for sheet {SheetId}", sheet.Id);
            }
        }
    }

    private async Task ProcessSheetColumns(ChannelSheet sheet)
    {
        var spreadsheet = await GetSpreadsheetWithData(sheet.SpreadSheetId);
        if (spreadsheet == null) return;

        var targetSheet = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.SheetId == sheet.Id);
        if (targetSheet == null)
        {
            _logger.LogWarning("Sheet not found with the given SheetId {SheetId}", sheet.Id);
            return;
        }

        var sheetData = targetSheet.Data?.FirstOrDefault();
        if (sheetData?.RowData == null || !sheetData.RowData.Any())
        {
            _logger.LogWarning("No row data found in sheet {SheetId}", sheet.Id);
            return;
        }

        var headerRow = sheetData.RowData.FirstOrDefault();
        if (headerRow?.Values == null)
        {
            _logger.LogWarning("Header row is empty in sheet {SheetId}", sheet.Id);
            return;
        }

        var requests = new List<Request>();
        
        // Process column E
        if (headerRow.Values.Count > 4)
        {
            var cellE = headerRow.Values[4];
            if (cellE?.FormattedValue == "Подписчиков 7+ дней(% от всего вступивших)")
            {
                requests.Add(CreateColumnERenameRequest(sheet.Id));
            }
        }

        // Process column 8
        if (headerRow.Values.Count > 8 && string.IsNullOrWhiteSpace(headerRow.Values[8]?.FormattedValue))
        {
            requests.Add(CreateDeleteColumnRequest(sheet.Id, 8));
        }

        // Add new column
        requests.Add(CreateInsertColumnRequest(sheet.Id, 12));
        requests.Add(CreateNewColumnHeaderRequest(sheet.Id, 12));

        if (requests.Any())
        {
            await ExecuteBatchUpdate(sheet.SpreadSheetId, requests);
        }
    }

    private async Task<Spreadsheet> GetSpreadsheetWithData(string spreadsheetId)
    {
        try
        {
            var getRequest = _sheetsService.Spreadsheets.Get(spreadsheetId);
            getRequest.IncludeGridData = true;
            return await getRequest.ExecuteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get spreadsheet {SpreadsheetId}", spreadsheetId);
            return null;
        }
    }

    private Request CreateColumnERenameRequest(int sheetId) => new()
    {
        UpdateCells = new UpdateCellsRequest
        {
            Range = new GridRange
            {
                SheetId = sheetId,
                StartRowIndex = 0,
                EndRowIndex = 1,
                StartColumnIndex = 4,
                EndColumnIndex = 5
            },
            Rows = new List<RowData>
            {
                new()
                {
                    Values = new List<CellData>
                    {
                        new() { UserEnteredValue = new ExtendedValue { StringValue = "Креатив" } }
                    }
                }
            },
            Fields = "userEnteredValue"
        }
    };

    private Request CreateDeleteColumnRequest(int sheetId, int columnIndex) => new()
    {
        DeleteDimension = new DeleteDimensionRequest
        {
            Range = new DimensionRange
            {
                SheetId = sheetId,
                Dimension = "COLUMNS",
                StartIndex = columnIndex,
                EndIndex = columnIndex + 1
            }
        }
    };

    private Request CreateInsertColumnRequest(int sheetId, int columnIndex) => new()
    {
        InsertDimension = new InsertDimensionRequest
        {
            Range = new DimensionRange
            {
                SheetId = sheetId,
                Dimension = "COLUMNS",
                StartIndex = columnIndex,
                EndIndex = columnIndex + 1
            }
        }
    };

    private Request CreateNewColumnHeaderRequest(int sheetId, int columnIndex) => new()
    {
        UpdateCells = new UpdateCellsRequest
        {
            Start = new GridCoordinate
            {
                SheetId = sheetId,
                RowIndex = 0,
                ColumnIndex = columnIndex
            },
            Rows = new List<RowData>
            {
                new()
                {
                    Values = new List<CellData>
                    {
                        new()
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
    };

    public async Task CreateSheet(long channelId, string sheetName, string spreadSheetName, long userId)
    {
        if (await SheetExists(channelId, userId))
        {
            return;
        }

        var spreadSheetEntity = await GetOrCreateSpreadSheet(userId, spreadSheetName);
        var sheet = await CreateSheetInSpreadsheet(spreadSheetEntity.Id, sheetName);
        
        var sheetEntity = new ChannelSheet
        {
            Id = sheet.Replies.First().AddSheet!.Properties!.SheetId!.Value,
            ChannelId = channelId,
            SpreadSheet = spreadSheetEntity
        };

        await _context.AddAsync(sheetEntity);
        await _context.SaveChangesAsync();

        await AppendRow(userId, channelId, GetDefaultHeaders());
    }

    private async Task<bool> SheetExists(long channelId, long userId)
    {
        return await _context.ChannelSheets
            .AnyAsync(s => s.ChannelId == channelId && s.SpreadSheet.UserId == userId);
    }

    private async Task<UserSpreadSheet> GetOrCreateSpreadSheet(long userId, string spreadSheetName)
    {
        var spreadSheetEntity = await _context.SpreadSheets
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (spreadSheetEntity != null)
        {
            return spreadSheetEntity;
        }

        var spreadSheet = await CreateNewSpreadsheet(spreadSheetName);
        spreadSheetEntity = new UserSpreadSheet
        {
            Id = spreadSheet.SpreadsheetId,
            UserId = userId,
        };

        return spreadSheetEntity;
    }

    private async Task<Spreadsheet> CreateNewSpreadsheet(string spreadSheetName)
    {
        var spreadSheetRequest = _sheetsService.Spreadsheets.Create(new Spreadsheet
        {
            Properties = new SpreadsheetProperties
            {
                Title = $"zakup_{spreadSheetName}"
            }
        });

        var spreadSheet = await spreadSheetRequest.ExecuteAsync();
        await DriveShareFile(spreadSheet.SpreadsheetId);
        return spreadSheet;
    }

    private async Task<BatchUpdateSpreadsheetResponse> CreateSheetInSpreadsheet(string spreadSheetId, string sheetName)
    {
        var batchUpdateRequest = new BatchUpdateSpreadsheetRequest
        {
            Requests = new List<Request>
            {
                new()
                {
                    AddSheet = new AddSheetRequest
                    {
                        Properties = new SheetProperties
                        {
                            Title = sheetName
                        }
                    }
                }
            }
        };

        return await _sheetsService.Spreadsheets
            .BatchUpdate(batchUpdateRequest, spreadSheetId)
            .ExecuteAsync();
    }

    private List<object> GetDefaultHeaders() => new()
    {
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
    };

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

    public async Task AppendRow(long userId, long channelId, List<object> cells)
    {
        var sheetEntity = await GetChannelSheet(userId, channelId);
        if (sheetEntity == null)
        {
            var channel = await _context.Channels.FirstOrDefaultAsync(q => q.Id == channelId);
            if (channel == null) return;

            await CreateSheet(channelId, channel.Title, "stat", userId);
            sheetEntity = await GetChannelSheet(userId, channelId);
            if (sheetEntity == null) return;
        }

        var data = new ValueRange
        {
            Values = new List<IList<object>> { cells }
        };

        var request = _sheetsService.Spreadsheets.Values.Append(data, sheetEntity.SpreadSheetId, sheetEntity.Channel.Title);
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
        await request.ExecuteAsync();
    }

    private async Task<ChannelSheet> GetChannelSheet(long userId, long channelId)
    {
        return await _context.ChannelSheets
            .Include(s => s.SpreadSheet)
            .Include(s => s.Channel)
            .Where(s => s.SpreadSheet.UserId == userId)
            .FirstOrDefaultAsync(s => s.ChannelId == channelId);
    }

    public async Task AppendRowByHeaders(long userId, long channelId, Dictionary<string, object> dataByColumnName)
    {
        var sheetEntity = await GetChannelSheet(userId, channelId);
        if (sheetEntity == null)
        {
            _logger.LogWarning("SheetEntity not found for user {UserId} and channel {ChannelId}", userId, channelId);
            return;
        }

        var headerRow = await GetHeaderRow(sheetEntity);
        if (headerRow == null) return;

        var columnIndexDict = CreateColumnIndexDictionary(headerRow, dataByColumnName);
        var newRow = CreateNewRow(headerRow.Count, dataByColumnName, columnIndexDict);

        await AppendRowToSheet(sheetEntity, newRow);
    }

    private async Task<IList<object>> GetHeaderRow(ChannelSheet sheetEntity)
    {
        var headerRange = $"{sheetEntity.Channel.Title}!A1:Z1";
        try
        {
            var headerResponse = await _sheetsService.Spreadsheets.Values
                .Get(sheetEntity.SpreadSheetId, headerRange)
                .ExecuteAsync();

            if (headerResponse.Values == null || !headerResponse.Values.Any())
            {
                _logger.LogWarning("Sheet {SheetTitle} is empty", sheetEntity.Channel.Title);
                return null;
            }

            return headerResponse.Values[0];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading header row for sheet {SheetTitle}", sheetEntity.Channel.Title);
            return null;
        }
    }

    private Dictionary<string, int> CreateColumnIndexDictionary(IList<object> headerRow, Dictionary<string, object> dataByColumnName)
    {
        var columnIndexDict = new Dictionary<string, int>();
        for (int i = 0; i < headerRow.Count; i++)
        {
            var colName = headerRow[i]?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(colName) && dataByColumnName.ContainsKey(colName))
            {
                columnIndexDict[colName] = i;
            }
        }
        return columnIndexDict;
    }

    private object[] CreateNewRow(int maxColumnCount, Dictionary<string, object> dataByColumnName, Dictionary<string, int> columnIndexDict)
    {
        var newRow = new object[maxColumnCount];
        for (int i = 0; i < maxColumnCount; i++)
        {
            newRow[i] = "";
        }

        foreach (var pair in dataByColumnName)
        {
            if (columnIndexDict.TryGetValue(pair.Key, out var idx))
            {
                newRow[idx] = pair.Value;
            }
        }

        return newRow;
    }

    private async Task AppendRowToSheet(ChannelSheet sheetEntity, object[] newRow)
    {
        var data = new ValueRange
        {
            Values = new List<IList<object>> { newRow }
        };

        try
        {
            var appendRequest = _sheetsService.Spreadsheets.Values
                .Append(data, sheetEntity.SpreadSheetId, sheetEntity.Channel.Title);
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            await appendRequest.ExecuteAsync();
            _logger.LogInformation("Row appended successfully to sheet {SheetTitle}", sheetEntity.Channel.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error appending row to sheet {SheetTitle}", sheetEntity.Channel.Title);
        }
    }

    public IQueryable<ChannelMember> GetMembersByInviteLink(string inviteLink)
    {
        return _context.ChannelMembers.Where(m => m.InviteLink == inviteLink);
    }

    public async Task UpdateCreativeTitles()
    {
        var sheets = await GetChannelSheetsWithIncludes();
        
        foreach (var sheet in sheets)
        {
            await UpdateCreativeTitlesForSheet(sheet);
        }
    }

    private async Task UpdateCreativeTitlesForSheet(ChannelSheet sheet)
    {
        var range = $"{sheet.Channel.Title}!A2:O";
        ValueRange response;

        try
        {
            response = await _sheetsService.Spreadsheets.Values
                .Get(sheet.SpreadSheet.Id, range)
                .ExecuteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data for sheet {SheetTitle}", sheet.Channel.Title);
            return;
        }

        if (response.Values == null || !response.Values.Any())
        {
            _logger.LogWarning("No data available in sheet {SheetTitle}", sheet.Channel.Title);
            return;
        }

        var updates = await ProcessCreativeTitleUpdates(sheet, response.Values);
        if (updates.Any())
        {
            await ExecuteBatchUpdate(sheet.SpreadSheet.Id, updates);
        }
    }

    private async Task<List<ValueRange>> ProcessCreativeTitleUpdates(ChannelSheet sheet, IList<IList<object>> values)
    {
        var updates = new List<ValueRange>();
        
        for (int i = 0; i < values.Count; i++)
        {
            var row = values[i];
            if (row.Count < 13 || row[12] == null)
            {
                _logger.LogWarning("Row {RowIndex} has insufficient data in sheet {SheetTitle}", 
                    i + 2, sheet.Channel.Title);
                continue;
            }

            var inviteLink = row[12].ToString();
            var zakup = await _context.TelegramZakups
                .Include(z => z.AdPost)
                .FirstOrDefaultAsync(z => z.InviteLink == inviteLink);

            if (zakup?.AdPost?.Title != null && (row.Count <= 4 || string.IsNullOrEmpty(row[4]?.ToString())))
            {
                var updateValues = new List<object>(row);
                while (updateValues.Count <= 4) updateValues.Add(null);
                updateValues[4] = zakup.AdPost.Title;

                var updateRange = $"{sheet.Channel.Title}!A{2 + i}:O{2 + i}";
                updates.Add(new ValueRange 
                { 
                    Range = updateRange, 
                    Values = new List<IList<object>> { updateValues } 
                });
            }
        }

        return updates;
    }

    public async Task UpdateStatistic(long userId)
    {
        var sheets = await GetUserSheets(userId);
        if (!sheets.Any())
        {
            _logger.LogWarning("No sheets found for userId={UserId}", userId);
            return;
        }

        var spreadsheetId = sheets.First().SpreadSheetId;
        if (string.IsNullOrEmpty(spreadsheetId))
        {
            _logger.LogWarning("Spreadsheet ID is empty for userId={UserId}", userId);
            return;
        }

        var spreadsheet = await GetSpreadsheet(spreadsheetId);
        if (spreadsheet == null) return;

        var validSheets = await GetValidSheets(sheets, spreadsheet);
        if (!validSheets.Any())
        {
            _logger.LogWarning("No valid sheets found for userId={UserId}", userId);
            return;
        }

        var batchGetResponse = await GetSheetValues(spreadsheetId, validSheets);
        if (batchGetResponse == null) return;

        var allInviteLinks = CollectInviteLinks(batchGetResponse, validSheets);
        var statsDictionary = await GatherStatsForInviteLinks(allInviteLinks.ToList());
        
        await UpdateSheetStatistics(spreadsheetId, batchGetResponse, validSheets, statsDictionary);
    }

    private async Task<List<ChannelSheet>> GetUserSheets(long userId)
    {
        return await _context.ChannelSheets
            .Include(s => s.SpreadSheet)
            .Include(s => s.Channel)
            .Where(s => s.SpreadSheet.UserId == userId)
            .ToListAsync();
    }

    private async Task<List<ChannelSheet>> GetValidSheets(List<ChannelSheet> sheets, Spreadsheet spreadsheet)
    {
        var googleSheetTitles = spreadsheet.Sheets
            .Select(s => s.Properties.Title)
            .Where(t => !string.IsNullOrEmpty(t))
            .ToHashSet();

        return sheets
            .Where(s => googleSheetTitles.Contains(s.Channel.Title))
            .ToList();
    }

    private async Task<BatchGetValuesResponse> GetSheetValues(string spreadsheetId, List<ChannelSheet> validSheets)
    {
        var validRanges = validSheets
            .Select(s => $"{s.Channel.Title}!A1:Z1000")
            .ToList();

        try
        {
            var batchGetRequest = _sheetsService.Spreadsheets.Values.BatchGet(spreadsheetId);
            batchGetRequest.Ranges = validRanges;
            return await batchGetRequest.ExecuteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sheet values for spreadsheet {SpreadsheetId}", spreadsheetId);
            return null;
        }
    }

    private HashSet<string> CollectInviteLinks(BatchGetValuesResponse batchGetResponse, List<ChannelSheet> validSheets)
    {
        var allInviteLinks = new HashSet<string>();

        for (int i = 0; i < batchGetResponse.ValueRanges.Count; i++)
        {
            var valueRange = batchGetResponse.ValueRanges[i];
            var sheetEntity = validSheets[i];

            if (valueRange.Values == null || !valueRange.Values.Any())
                continue;

            var headerRow = valueRange.Values[0];
            int inviteLinkIndex = FindColumnIndex(headerRow, INVITE_LINK_COLUMN);
            if (inviteLinkIndex == -1)
            {
                _logger.LogWarning("Sheet '{SheetTitle}' does not contain invite-link column", 
                    sheetEntity.Channel.Title);
                continue;
            }

            CollectInviteLinksFromRows(valueRange.Values, inviteLinkIndex, allInviteLinks);
        }

        return allInviteLinks;
    }

    private void CollectInviteLinksFromRows(IList<IList<object>> values, int inviteLinkIndex, HashSet<string> allInviteLinks)
    {
        for (int rowIndex = 1; rowIndex < values.Count; rowIndex++)
        {
            var row = values[rowIndex];
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

    private async Task UpdateSheetStatistics(
        string spreadsheetId, 
        BatchGetValuesResponse batchGetResponse,
        List<ChannelSheet> validSheets,
        Dictionary<string, Dictionary<string, int>> statsDictionary)
    {
        var allUpdates = new List<ValueRange>();

        for (int i = 0; i < batchGetResponse.ValueRanges.Count; i++)
        {
            var valueRange = batchGetResponse.ValueRanges[i];
            var sheetEntity = validSheets[i];
            
            if (valueRange.Values == null || valueRange.Values.Count < 1)
                continue;

            var headerRow = valueRange.Values[0];
            int inviteLinkIndex = FindColumnIndex(headerRow, INVITE_LINK_COLUMN);
            if (inviteLinkIndex == -1)
                continue;

            var columnIndexDict = CreateColumnIndexDictionary(headerRow, ColumnStatsMap);
            int priceIndex = FindColumnIndex(headerRow, PRICE_COLUMN);

            await ProcessSheetRows(
                valueRange.Values, 
                sheetEntity, 
                inviteLinkIndex, 
                priceIndex, 
                columnIndexDict, 
                statsDictionary, 
                allUpdates);
        }

        if (allUpdates.Any())
        {
            await ExecuteBatchUpdate(spreadsheetId, allUpdates);
        }
    }

    private async Task ProcessSheetRows(
        IList<IList<object>> values,
        ChannelSheet sheetEntity,
        int inviteLinkIndex,
        int priceIndex,
        Dictionary<string, int> columnIndexDict,
        Dictionary<string, Dictionary<string, int>> statsDictionary,
        List<ValueRange> allUpdates)
    {
        for (int rowIndex = 1; rowIndex < values.Count; rowIndex++)
        {
            var row = values[rowIndex];
            if (row.Count <= inviteLinkIndex) 
                continue;

            var linkObj = row[inviteLinkIndex];
            if (linkObj == null) 
                continue;

            var inviteLink = linkObj.ToString();
            if (string.IsNullOrEmpty(inviteLink))
                continue;

            if (!statsDictionary.TryGetValue(inviteLink, out var stats))
                continue;

            decimal price = 0;
            if (priceIndex >= 0 && row.Count > priceIndex)
                decimal.TryParse(row[priceIndex]?.ToString(), out price);

            var currentInChannel = stats["currentInChannel"];
            var leftTotal = stats["leftTotal"];
            var pricePerSubscriber = (currentInChannel > 0) ? (price / currentInChannel) : 0m;
            var leftRate48H = (leftTotal > 0) 
                ? (double)stats["leftWithin48Hours"] / leftTotal * 100 
                : 0;

            var rowValuesForUpdate = new Dictionary<string, object>
            {
                { "currentInChannel",   currentInChannel },
                { "leftChannel",        stats["leftChannel"] },
                { "pricePerSubscriber", pricePerSubscriber },
                { "leftRateFirst48H",   leftRate48H },
                { "premiumUsers",       stats["premiumUsers"] },
                { "clientCount",        stats["clientCount"] },
                { "commentersCount",    stats["commentersCount"] },
                { "spent7DaysPercent",  stats["spent7DaysPercent"] }
            };

            AddRowUpdates(sheetEntity, rowIndex, columnIndexDict, rowValuesForUpdate, allUpdates);
        }
    }

    private void AddRowUpdates(
        ChannelSheet sheetEntity,
        int rowIndex,
        Dictionary<string, int> columnIndexDict,
        Dictionary<string, object> rowValuesForUpdate,
        List<ValueRange> allUpdates)
    {
        foreach (var (colName, statKey) in ColumnStatsMap)
        {
            if (!columnIndexDict.TryGetValue(colName, out var idx) || idx < 0)
                continue;

            var newValue = rowValuesForUpdate[statKey];
            var realRowNumber = rowIndex + 1;
            var columnLetter = ColumnIndexToLetter(idx);
            var cellRange = $"{sheetEntity.Channel.Title}!{columnLetter}{realRowNumber}";

            allUpdates.Add(new ValueRange
            {
                Range = cellRange,
                Values = new List<IList<object>> { new List<object> { newValue } }
            });
        }
    }

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

    private string ColumnIndexToLetter(int columnIndex)
    {
        columnIndex++;
        string columnLetter = "";
        while (columnIndex > 0)
        {
            int remainder = (columnIndex - 1) % 26;
            columnLetter = (char)(remainder + 'A') + columnLetter;
            columnIndex = (columnIndex - 1) / 26;
        }
        return columnLetter;
    }

    private async Task ExecuteBatchUpdate(string spreadsheetId, List<ValueRange> updates)
    {
        var requestBody = new BatchUpdateValuesRequest
        {
            ValueInputOption = "USER_ENTERED",
            Data = updates
        };

        try
        {
            await _sheetsService.Spreadsheets.Values
                .BatchUpdate(requestBody, spreadsheetId)
                .ExecuteAsync();
            _logger.LogInformation("Successfully updated spreadsheet {SpreadsheetId}", spreadsheetId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute batch update for spreadsheet {SpreadsheetId}", spreadsheetId);
            throw;
        }
    }

    private Dictionary<string, int> CreateColumnIndexDictionary(IList<object> headerRow, Dictionary<string, string> columnMap)
    {
        var columnIndexDict = new Dictionary<string, int>();
        for (int i = 0; i < headerRow.Count; i++)
        {
            var colName = headerRow[i]?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(colName) && columnMap.ContainsKey(colName))
            {
                columnIndexDict[colName] = i;
            }
        }
        return columnIndexDict;
    }

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
}
