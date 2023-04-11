#nullable enable
namespace MassTransit.Serialization
{
    using System;
    using System.Linq;
    using System.Net.Mime;
    using System.Text.Json;


    public class SystemTextJsonRawSerializerContext :
        SystemTextJsonSerializerContext
    {
        readonly RawSerializerOptions _rawOptions;

        public SystemTextJsonRawSerializerContext(IObjectDeserializer objectDeserializer, JsonSerializerOptions options, ContentType contentType,
            MessageContext messageContext, string[] messageTypes, RawSerializerOptions rawOptions, JsonElement message)
            : base(objectDeserializer, options, contentType, messageContext, messageTypes, message: message)
        {
            _rawOptions = rawOptions;
        }

        public override IMessageSerializer GetMessageSerializer()
        {
            return new SystemTextJsonBodyMessageSerializer(Message, ContentType, Options, _rawOptions);
        }

        public override bool IsSupportedMessageType<T>()
        {
            var typeUrn = MessageUrn.ForTypeString<T>();

            return true;
        }

        public override bool IsSupportedMessageType(Type messageType)
        {
            var typeUrn = MessageUrn.ForTypeString(messageType);

            return true;
        }
    }
}
