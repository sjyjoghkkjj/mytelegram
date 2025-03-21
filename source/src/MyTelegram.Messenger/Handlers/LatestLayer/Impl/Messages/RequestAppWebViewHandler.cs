// ReSharper disable All

namespace MyTelegram.Messenger.Handlers.LatestLayer.Impl.Messages;

///<summary>
/// Open a <a href="https://corefork.telegram.org/bots/webapps">bot web app</a> from a <a href="https://corefork.telegram.org/api/links#named-bot-web-app-links">named bot web app deep link</a>, sending over user information after user confirmation.After calling this method, until the user closes the webview, <a href="https://corefork.telegram.org/method/messages.prolongWebView">messages.prolongWebView</a> must be called every 60 seconds.
/// See <a href="https://corefork.telegram.org/method/messages.requestAppWebView" />
///</summary>
internal sealed class RequestAppWebViewHandler : RpcResultObjectHandler<MyTelegram.Schema.Messages.RequestRequestAppWebView, MyTelegram.Schema.IWebViewResult>,
    Messages.IRequestAppWebViewHandler
{
    protected override Task<MyTelegram.Schema.IWebViewResult> HandleCoreAsync(IRequestInput input,
        MyTelegram.Schema.Messages.RequestRequestAppWebView obj)
    {
        throw new NotImplementedException();
    }
}
