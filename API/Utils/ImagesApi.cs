﻿using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Models;
using ShockLink.Common.Models;
using ShockLink.Common.ShockLinkDb;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace ShockLink.API.Utils;

public static class ImagesApi
{
    private static readonly ILogger Logger = ApplicationLogging.CreateLogger(typeof(ImagesApi));

    private static readonly HttpClient HttpClient = new()
    {
        DefaultRequestHeaders =
        {
            Authorization = new AuthenticationHeaderValue("Bearer", ApiConfig.CloudflareImagesKey)
        },
        BaseAddress =
            new Uri($"https://api.cloudflare.com/client/v4/accounts/{ApiConfig.CloudflareAccountId}/")
    };

    public static async Task<bool> UploadAvatar(Guid userId, Stream stream, ShockLinkContext db)
    {
        Logger.LogTrace("Uploading image to cloudflare");
        var msg = new HttpRequestMessage(HttpMethod.Post, "images/v1");
        
        
        msg.Content = new MultipartFormDataContent
        {
            { new StreamContent(stream), "\"file\"", $"\"dev-user_{userId}\"" }
        };

        var res = await HttpClient.SendAsync(msg);
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Cloudflare API response for image create. Status Code: {StatusCode}, Response: {Response}",
                res.StatusCode, await res.Content.ReadAsStringAsync());
        
        if (res.StatusCode == HttpStatusCode.UnprocessableEntity)
            throw new IncorrectImageFormatException("Image format must be PNG, JPEG, GIF, WebP or SVG");
        if (!res.IsSuccessStatusCode)
        {
            Logger.LogCritical(
                "Cloudflare API error during image creation. Status Code: {StatusCode}, Response: {Response}",
                res.StatusCode, await res.Content.ReadAsStringAsync());
            return false;
        }
        
        var json = JsonSerializer.Deserialize<CloudflareImagePost>(await res.Content.ReadAsStringAsync());
        if (json == null) throw new JsonException("Json deserialization failed");
        
        Logger.LogTrace("Making new db entry and setting as active avatar");
        db.CfImages.Add(new CfImage
        {
            Id = json.Result.Id,
            CreatedBy = userId,
            Type = CfImageType.Avatar
        });

        var user = await db.Users.SingleAsync(x => x.Id == userId);
        user.Image = json.Result.Id;

        await db.SaveChangesAsync();
        
        return true;
    }

    public static async Task<bool> DeleteImage(Guid id, ShockLinkContext db)
    {
        Logger.LogTrace("Deleting image from cloudflare");
        var msg = new HttpRequestMessage(HttpMethod.Delete, $"images/v1/{id}");
        var res = await HttpClient.SendAsync(msg);

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Cloudflare API response for image delete. Status Code: {StatusCode}, Response: {Response}",
                res.StatusCode, await res.Content.ReadAsStringAsync());

        if (res.StatusCode != HttpStatusCode.NotFound && !res.IsSuccessStatusCode)
        {
            Logger.LogCritical(
                "Cloudflare API error during image deletion. Status Code: {StatusCode}, Response: {Response}",
                res.StatusCode, await res.Content.ReadAsStringAsync());
            return false;
        }
        Logger.LogTrace("Deleting image from db");
        await db.CfImages.Where(x => x.Id == id).ExecuteDeleteAsync();
        return true;
    }

    internal class IncorrectImageFormatException : Exception
    {
        public IncorrectImageFormatException(string message) : base(message)
        {
        }
    }
}