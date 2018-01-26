using System.Collections.Generic;

namespace Minecraft_modsTranslate
{
    public class Ini
    {
        private List<Line> _lines = new List<Line>();
        

        public List<Line> Lines
        {
            get
            {
                return _lines;
            }

            set
            {
                _lines = value;
            }
        }
        public class Line
        {
            string _section;
            string _key;
            string _value;
            public string Section
            {
                get
                {
                    return _section;
                }

                set
                {
                    _section = value;
                }
            }

            public string Key
            {
                get
                {
                    return _key;
                }

                set
                {
                    _key = value;
                }
            }

            public string Value
            {
                get
                {
                    return _value;
                }

                set
                {
                    _value = value;
                }
            }
        }
    }
}
