namespace EIS.Domain.Entities
{
    public class Payload
    {
        public List<ChangedAttributes> ChangedAttributes { get; set;}
        public object Content { get; set;}
        public object OldContent { get; set;}
        public string ContentType { get; set;}
        public string SourceSystemName { get; set;}

        public Payload()
        {
        }

        public Payload(object content)
        {
            Content = content;
        }

        public Payload(object content, string contentType, string sourceSystemName)
        {
            Content = content;
            ContentType = contentType;
            SourceSystemName = sourceSystemName;
        }

        public TOutput ConvertContent<TOutput>(object payloadContent)
        {
            return (TOutput)JsonSerializerUtil.Deserialize(payloadContent.ToString(), typeof(TOutput));
        }
    }
}