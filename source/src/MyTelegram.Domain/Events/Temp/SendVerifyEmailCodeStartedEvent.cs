namespace MyTelegram.Domain.Events.Temp;

public class VerifyEmailCodeStartedEvent(
    RequestInfo requestInfo,
    long userId,
    string code,
    string codeForSignIn,
    AppCodeType appCodeType
    ) : RequestAggregateEvent2<TempAggregate, TempId>(requestInfo)
{
    public long UserId { get; } = userId;
    public string Code { get; } = code;
    public string CodeForSignIn { get; } = codeForSignIn;
    public AppCodeType AppCodeType { get; } = appCodeType;
}