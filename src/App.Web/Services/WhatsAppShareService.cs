using Microsoft.JSInterop;

namespace App.Web.Services;

public class WhatsAppShareService : IWhatsAppShareService
{
    private readonly IJSRuntime _js;

    public WhatsAppShareService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<bool> ShareFileAsync(byte[] fileBytes, string fileName, string contentType, string text)
    {
        var base64 = Convert.ToBase64String(fileBytes);
        return await _js.InvokeAsync<bool>("shareFile", fileName, base64, contentType, text);
    }
}
