using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.EntityFramework;
using Zakup.Services.Extensions;

namespace Zakup.Services;

public class DocumentsStorageService
{
    private readonly FileStorageService _fileStorageService;
    
    private readonly ApplicationDbContext _context;
    
    public DocumentsStorageService(FileStorageService fileStorageService, ApplicationDbContext context)
    {
        _fileStorageService = fileStorageService;
        _context = context;
    }

    public async Task<Stream> GetDocument(Guid documentid)
    {
        return await _fileStorageService.GetFile(documentid.ToString());
    }
    
    public async Task SaveDocuments(ITelegramBotClient botClient, Message message, string mediaGroupId)
    {
        if (message.Photo != null)
        {
            var documntId = await SaveDocument(message.Photo.Last(),  botClient, TelegramDocumentKind.IMAGE, "image/jpeg", message.MediaGroupId);
            await AttachFileToMediaGroup(documntId, mediaGroupId);
        }

        if (message.Video != null)
        {
            var documntId =await SaveDocument( message.Video ,  botClient, TelegramDocumentKind.VIDEO, "video/mp4", message.MediaGroupId, message?.Video?.Thumbnail?.FileId );
            await AttachFileToMediaGroup(documntId, mediaGroupId);
        }
        
        if (message.Animation != null)
        {
            var documntId =await SaveDocument( message.Animation ,  botClient, TelegramDocumentKind.GIF, "video/gif", message.MediaGroupId, message?.Animation?.Thumbnail?.FileId );
            await AttachFileToMediaGroup(documntId, mediaGroupId);
        }

        if (message.Document != null)
        {
            var documntId =await SaveDocument( message.Document ,  botClient, TelegramDocumentKind.DOCUMENT, null, message.MediaGroupId, message?.Document?.Thumbnail?.FileId );
            await AttachFileToMediaGroup(documntId, mediaGroupId);
        }
    }


    public async Task AttachFileToMediaGroup(Guid fileId, string mediaGroupId)
    {
        await CreateMediaGroupIfNotExist(mediaGroupId);
        var relation = new FileMediaGroup
        {
            FileId = fileId,
            MediaGroupId = mediaGroupId,
        };
        await _context.FileMediaGroups.AddAsync(relation);
        await _context.SaveChangesAsync();
    }
    
    public async Task AttachMediaGroupToPost(string mediaGroupId, Guid postId)
    {
        await CreateMediaGroupIfNotExist(mediaGroupId);
        var post = await _context.TelegramAdPosts.FirstOrDefaultAsync(q => q.Id == postId);
        post.MediaGroupId = mediaGroupId;
        _context.Update(post);
        await _context.SaveChangesAsync();
         
    }

    private async Task CreateMediaGroupIfNotExist(string mediaGroupId)
    {
        var mediaGroup = await _context.MediaGroups.FirstOrDefaultAsync(q => q.MediaGroupId == mediaGroupId);
        if (mediaGroup == null)
        {
            mediaGroup = new MediaGroup()
            {
                MediaGroupId = mediaGroupId
            };
            await _context.MediaGroups.AddAsync(mediaGroup);
            await _context.SaveChangesAsync();
        }
    }
    private async Task<Guid> SaveDocument(FileBase file, ITelegramBotClient botClient, TelegramDocumentKind kind, string fileType, string mediaGroupId, string thumbnailId = null)
    {
        var document = await _context.TelegramDocuments.FirstOrDefaultAsync(q => q.FileId == file.FileId);
        
        await _context.ExecuteInTransactionAsync(async () =>
        {
            if (document == null)
            {
                var newDocument = new TelegramDocument
                {
                    Id = Guid.NewGuid(),
                    FileId = file.FileId,
                    Kind = kind,
                    ThumbnailId = thumbnailId,
                };
                document = (await _context.TelegramDocuments.AddAsync(newDocument)).Entity;
                    
                var mem = new MemoryStream();
        
                var fileInfo = await botClient.GetFileAsync(file.FileId);
                await botClient.DownloadFileAsync(fileInfo.FilePath!, mem);
                    
                await _fileStorageService.UploadFile(mem, document.Id.ToString(), fileType);
            }
                
            await _context.SaveChangesAsync();
            
        });
        
        return document!.Id;
    }
    
}