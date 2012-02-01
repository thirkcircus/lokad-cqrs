using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lokad.Cqrs;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Evil;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Sample.Wires
{
    public static class Contracts
    {
        static Type[] LoadMessageContracts()
        {
            var messages = new[] { typeof(UserCreated), typeof(SampleEntityVector) }
                .SelectMany(t => t.Assembly.GetExportedTypes())
                .Where(t => typeof(ISampleMessage).IsAssignableFrom(t))
                .Where(t => !t.IsAbstract)
                .ToArray();
            return messages;
        }

        public static IEnvelopeStreamer CreateStreamer()
        {
            return new EnvelopeStreamer(new DataSerializer(LoadMessageContracts()), new EnvelopeSerializer());
        }

        sealed class EnvelopeSerializer : IEnvelopeSerializer
        {
            public void SerializeEnvelope(Stream stream, EnvelopeContract c)
            {
                Serializer.Serialize(stream, c);
            }

            public EnvelopeContract DeserializeEnvelope(Stream stream)
            {
                return Serializer.Deserialize<EnvelopeContract>(stream);
            }
        }

        class DataSerializer : AbstractDataSerializer
        {
            public DataSerializer(ICollection<Type> knownTypes)
                : base(knownTypes)
            {
                RuntimeTypeModel.Default[typeof(DateTimeOffset)].Add("m_dateTime", "m_offsetMinutes");
            }

            protected override Formatter PrepareFormatter(Type type)
            {
                var name = ContractEvil.GetContractReference(type);
                var formatter = RuntimeTypeModel.Default.CreateFormatter(type);
                return new Formatter(name, formatter.Deserialize, (o, stream) => formatter.Serialize(stream, o));
            }
        }

    }
}