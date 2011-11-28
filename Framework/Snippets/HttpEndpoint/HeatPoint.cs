using System.Runtime.Serialization;

namespace Snippets.HttpEndpoint
{
    [DataContract]
    public class HeatPoint
    {
        [DataMember]
        public byte Intensity { get; set; }
        [DataMember]
        public int X { get; set; }
        [DataMember]
        public int Y { get; set; }

        public HeatPoint(int iX, int iY, byte bIntensity)
        {
            X = iX;
            Y = iY;
            Intensity = bIntensity;
        }
    }
}