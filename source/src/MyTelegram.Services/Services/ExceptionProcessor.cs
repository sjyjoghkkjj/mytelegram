using EventFlow.Exceptions;
using MyTelegram.Schema;

namespace MyTelegram.Services.Services;

public class ExceptionProcessor(
    ILogger<ExceptionProcessor> logger,
    IObjectMessageSender objectMessageSender,
    IEventBus eventBus)
    : IExceptionProcessor, ITransientDependency
{
    public Task HandleExceptionAsync(Exception ex, IRequestInput input, IObject? requestData, string? handlerName)
    {
        logger.LogError(ex,
            "Process request failed, handler: {HandlerName}, userId: {UserId}, requestInput: {@RequestInput}, requestData: {@RequestData}",
            handlerName,
            input.UserId,
            input,
            requestData
        );
        return ProcessExceptionCoreAsync(ex, input);
    }

    private async Task ProcessExceptionCoreAsync(Exception ex, IRequestInput input)
    {
        string errorMessage;
        int errorCode;
        switch (ex)
        {
            case DuplicateOperationException:
                var eventData = new DuplicateCommandEvent(input.PermAuthKeyId, input.UserId, input.ReqMsgId);
                await eventBus.PublishAsync(eventData);
                return;
            //break;
            case NotImplementedException:
                errorCode = MyTelegramServerDomainConsts.InternalErrorCode;
                errorMessage = "API NotImplemented";
                break;

            case RpcException rpcException:
                errorCode = rpcException.RpcError.ErrorCode;
                errorMessage = rpcException.RpcError.Message;
                break;

            case DomainError domainError:
                errorCode = MyTelegramServerDomainConsts.InternalErrorCode;
                errorMessage = domainError.Message;
                break;

            case SagaPublishException sagaPublishException:
                var innerException = sagaPublishException.InnerException;
                errorMessage = innerException switch
                {
                    CommandException { InnerException: RpcException subInnerException } => subInnerException
                        .Message,
                    _ => MyTelegramServerDomainConsts.InternalErrorMessage
                };
                errorCode = MyTelegramServerDomainConsts.BadRequestErrorCode;
                break;

            default:
                errorCode = MyTelegramServerDomainConsts.InternalErrorCode;
                errorMessage = MyTelegramServerDomainConsts.InternalErrorMessage;
                break;
        }

        var rpcError = new TRpcError { ErrorCode = errorCode, ErrorMessage = errorMessage };
        var rpcResult = new TRpcResult { ReqMsgId = input.ReqMsgId, Result = rpcError };
        await objectMessageSender.SendMessageToPeerAsync(input.ToRequestInfo(), rpcResult);
    }
}