namespace WebpInter
{
    public interface IImage2Webp
    {
        public byte[] Get(string Ip);

        public byte[] Get2(string Ip);

        public string Send(byte[] data, string status, string md5, long size);

        public Tuple<byte[], string, string> Get3(string MD5);

        public void Set(string ip, string text, byte[] Data);
    }
}