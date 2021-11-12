namespace GloomServer
{
    public class ServerConfiguration
    {
        public int Port { get; set; } = 5000;
        public string Ip { get; set; } = "0.0.0.0";
        public string Certificate { get; set; } = string.Empty;
        public string CertificatePassword { get; set; } = string.Empty;
    }
}
