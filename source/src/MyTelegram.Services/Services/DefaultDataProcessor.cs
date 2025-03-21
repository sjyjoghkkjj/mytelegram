using MyTelegram.Schema;
using System.Diagnostics;

namespace MyTelegram.Services.Services;

public class DefaultDataProcessor<TData>(
    IHandlerHelper handlerHelper,
    IObjectMessageSender objectMessageSender,
    IRpcResultCacheAppService rpcResultCacheAppService,
    ILogger<DefaultDataProcessor<TData>> logger,
    IExceptionProcessor exceptionProcessor,
    IRequestHelper requestHelper,
    IInvokeAfterMsgProcessor invokeAfterMsgProcessor)
    : IDataProcessor<TData>
    where TData : DataReceivedEvent
{
    public virtual Task ProcessAsync(TData obj)
    {
        Task.Run(async () =>
        {
            var sw = Stopwatch.StartNew();
            if (handlerHelper.TryGetHandler(obj.ObjectId, out var handler))
            {
                //if (rpcResultCacheAppService.TryGetRpcResult(obj.UserId, obj.ReqMsgId, out var rpcResult))
                //{
                //    sw.Stop();
                //    await SendMessageToPeerAsync(GetRequestInput(obj).ToRequestInfo(), rpcResult);
                //    return;
                //}

                IObject? data = null;
                var req = GetRequestInput(obj);
                try
                {
                    data = GetData(obj);

                    bool needToCheckRequest = handler is IDistinctObjectHandler ||
                                              ObjectIdConsts.CommandObjectIdToNames.ContainsKey(req.ObjectId);

                    if (!needToCheckRequest && data is IHasSubQuery subQuery)
                    {
                        needToCheckRequest =
                            ObjectIdConsts.CommandObjectIdToNames.ContainsKey(subQuery.Query.ConstructorId);

                        if (!needToCheckRequest && subQuery.Query is IHasSubQuery subQuery2)
                        {
                            needToCheckRequest =
                                ObjectIdConsts.CommandObjectIdToNames.ContainsKey(subQuery2.Query.ConstructorId);
                        }
                    }

                    if (needToCheckRequest)
                    {
                        if (!await requestHelper.CheckRequestAsync(req))
                        {
                            return;
                        }
                    }

                    var handlerName = handler.GetType().Name;

                    if (data is IHasSubQuery)
                    {
                        handlerName = handlerHelper.GetHandlerFullName(data);
                    }

                    if (!needToCheckRequest)
                    {
                        Console.WriteLine($"not need to check request:{handlerName}");
                    }

                    var r = await handler.HandleAsync(req, data);
                    sw.Stop();

                    logger.LogInformation(
                        "User {UserId} {PermAuthKeyId} {Handler} {DeviceType}[{Layer}] {Timespan}ms",
                        obj.UserId,
                        req.PermAuthKeyId,
                        handlerName,
                        obj.DeviceType,
                        obj.Layer,
                        sw.Elapsed.TotalMilliseconds
                    );

                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug(
                            "User {UserId} {Handler} {DeviceType}[{Layer}] {Timespan}ms. [{@Input}]Request data: {@Request}, response: {@Response}",
                            obj.UserId,
                            handlerName,
                            obj.DeviceType,
                            obj.Layer,
                            sw.Elapsed.TotalMilliseconds,
                            req,
                            data,
                            r
                        );
                    }

                    if (r != null!)
                    {
                        await SendMessageToPeerAsync(GetRequestInput(obj).ToRequestInfo(), r);
                    }

                    await invokeAfterMsgProcessor.AddCompletedReqMsgIdAsync(obj.ReqMsgId);
                }
                catch (Exception ex)
                {
                    await exceptionProcessor.HandleExceptionAsync(ex, req, data,
                        handler.GetType().Name);
                }
            }
        });

        return Task.CompletedTask;
    }

    protected virtual IObject GetData(TData obj)
    {
        return obj.Data.ToTObject<IObject>();
    }

    protected virtual IRequestInput GetRequestInput(TData obj)
    {
        var req = new RequestInput(
            obj.ConnectionId,
            obj.RequestId,
            obj.ObjectId,
            obj.ReqMsgId,
            obj.UserId,
            obj.AuthKeyId,
            obj.PermAuthKeyId,
            obj.Layer,
            obj.Date,
            obj.DeviceType,
            obj.ClientIp
        );

        return req;
    }

    protected virtual Task SendMessageToPeerAsync(RequestInfo requestInfo,
        IObject data)
    {
        return objectMessageSender.SendMessageToPeerAsync(requestInfo, data);
    }
}