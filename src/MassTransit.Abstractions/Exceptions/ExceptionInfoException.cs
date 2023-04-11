namespace MassTransit
{
    using System;
    using System.Collections;


    [Serializable]
    public class ExceptionInfoException :
        MassTransitException
    {
        readonly IDictionary? _data;

        public ExceptionInfoException(ExceptionInfo exceptionInfo)
            : base(exceptionInfo.Message, exceptionInfo.InnerException != null ? new ExceptionInfoException(exceptionInfo.InnerException) : default)
        {
            ExceptionInfo = exceptionInfo;
            if (ExceptionInfo.Data != null)
                _data = (IDictionary)ExceptionInfo.Data;
        }

        public ExceptionInfo ExceptionInfo { get; }

        public override string StackTrace => ExceptionInfo.StackTrace;
        public override string Source => ExceptionInfo.Source;

        public override IDictionary Data => _data ?? base.Data;
    }
}
