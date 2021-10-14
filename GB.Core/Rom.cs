using System.Text;

namespace GB.Core
{
    internal class Rom
    {
        private ReadOnlyMemory<byte> _romData = new();

        private string _title = "";
        private string _licensee = "";        
        
        private Rom() { }

        public static async Task<Rom?> FromFile(string path)
        {
            if (!File.Exists(path)) 
            {
                return null; 
            }

            await using var stream = File.OpenRead(path);
            var rom = new Rom();
            await rom.Initialize(stream);

            return rom;
        }

        private async Task Initialize(FileStream stream)
        {
            await using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            
            _romData = new ReadOnlyMemory<byte>(ms.ToArray());
        }

        public string Title
        {
            get
            {
                if (_title == "")
                {
                    _title = ASCIIEncoding.Default.GetString(_romData.Slice(0x134, 16).Span);
                }

                return _title;
            }
        }

        public string Licensee
        {
            get
            {
                if (_licensee == "")
                {
                    // read title
                }

                return _licensee;
            }
        }
    }
}