using Languages.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace Minesweeper
{
    public class Data
    {
        public enum Page { Main, Game, Congratulation };
        public Page page;

        //1, 2, 3 mean entering the champion's name now
        //-1 means there is no champion now
        public int championIdx = -1;

        public float scale, shift_x, shift_y;

        public ActionResult actionResult;

        public Settings settings;

        public long time;

        public GameMechanics game;

        public void NewGame()
        {
            game = new GameMechanics(settings.width, settings.height, settings.number_of_landmines);
            scale = -1;
            time = 0;
            actionResult = ActionResult.Continuation;
        }

        public static void Save(Data data)
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var stream = new IsolatedStorageFileStream("data.xml", FileMode.Create, FileAccess.Write, store))
                {
                    var settingsSerializer = new XmlSerializer(typeof(Data));
                    settingsSerializer.Serialize(stream, data);
                }
            }
        }

        public static void Load(out Data data)
        {
            data = new Data();
            data.settings = new Settings(0);

            try
            {
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var stream = new IsolatedStorageFileStream("data.xml", FileMode.OpenOrCreate, FileAccess.Read, store))
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            if (!reader.EndOfStream)
                            {
                                var serializer = new XmlSerializer(typeof(Data));
                                data = (Data)serializer.Deserialize(reader);
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }
    public class Champion : INotifyPropertyChanged
    {
        public enum Type { level1, level2, level3, levelx };

        public event PropertyChangedEventHandler PropertyChanged;

        public Champion(Type type)
        {
            this.type = type;
            name = AppResources.unknown;
            seconds = 999;
        }

        //for serializer
        public Champion() { }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
            }
        }

        [XmlIgnore]
        public string str_type
        {
            get
            {
                switch (type)
                {
                    case Type.level1: return AppResources.level1;
                    case Type.level2: return AppResources.level2;
                    case Type.level3: return AppResources.level3;
                }
                return "Error";
            }
        }

        public string name
        {
            get { return m_name; }
            set
            {
                m_name = value;
                NotifyPropertyChanged("name");
            }
        }

        public int seconds
        {
            get { return m_seconds; }
            set
            {
                m_seconds = value;
                NotifyPropertyChanged("seconds");
            }
        }

        public Type type { get; set; }

        private int m_seconds;
        private string m_name;
    }

    public class ChampionList : List<Champion> {}

    public class Settings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [XmlIgnore]
        public string version
        {
            get
            {
                AssemblyName assemblyName = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
                return assemblyName.Version.ToString();
            }
        }

        //for serializer
        public Settings() {}

        public Settings(int i)
        {
            m_mode = new bool[4];
            m_mode[0] = true;
            m_mode[1] = false;
            m_mode[2] = false;
            m_mode[3] = false;

            m_height = 10;
            m_width = 10;
            m_number_of_landmines = 10;
            m_soundEffects = true;

            champions = new ChampionList();
            champions.Add(new Champion(Champion.Type.level1));
            champions.Add(new Champion(Champion.Type.level2));
            champions.Add(new Champion(Champion.Type.level3));
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
            }
        }

        [XmlIgnore]
        public int height
        {
            get { return m_height; }
            set
            {
                m_height = value;
                levelx = true;
                NotifyPropertyChanged("height");
            }
        }

        [XmlIgnore]
        public int width
        {
            get { return m_width; }
            set
            {
                m_width = value;
                levelx = true;
                NotifyPropertyChanged("width");
            }
        }

        [XmlIgnore]
        public int number_of_landmines
        {
            get { return m_number_of_landmines; }
            set
            {
                m_number_of_landmines = value;
                levelx = true;
                NotifyPropertyChanged("number_of_landmines");
            }
        }

        [XmlIgnore]
        public bool level1
        {
            get { return m_mode[0]; }
            set
            {
                m_mode[0] = value;
                if (value)
                {
                    m_width = 10;
                    m_height = 10;
                    m_number_of_landmines = 10;
                    NotifyPropertyChanged("height");
                    NotifyPropertyChanged("width");
                    NotifyPropertyChanged("number_of_landmines");
                }
                NotifyPropertyChanged("level1");
            }
        }

        [XmlIgnore]
        public bool level2
        {
            get { return m_mode[1]; }
            set
            {
                m_mode[1] = value;
                if (value)
                {
                    m_width = 16;
                    m_height = 16;
                    m_number_of_landmines = 40;
                    NotifyPropertyChanged("height");
                    NotifyPropertyChanged("width");
                    NotifyPropertyChanged("number_of_landmines");
                }
                NotifyPropertyChanged("level2");
            }
        }

        [XmlIgnore]
        public bool level3
        {
            get { return m_mode[2]; }
            set
            {
                m_mode[2] = value;
                if (value)
                {
                    m_width = 30;
                    m_height = 16;
                    m_number_of_landmines = 99;
                    NotifyPropertyChanged("height");
                    NotifyPropertyChanged("width");
                    NotifyPropertyChanged("number_of_landmines");
                }
                NotifyPropertyChanged("level3");
            }
        }

        [XmlIgnore]
        public bool levelx
        {
            get { return m_mode[3]; }
            set
            {
                m_mode[3] = value;
                NotifyPropertyChanged("levelx");
            }
        }

        public bool soundEffects
        {
            get { return m_soundEffects; }
            set
            {
                m_soundEffects = value;
                NotifyPropertyChanged("soundEffects");
            }
        }
        
        public Champion.Type GetChampionType()
        {
            if (level1) return Champion.Type.level1;
            if (level2) return Champion.Type.level2;
            if (level3) return Champion.Type.level3;

            return Champion.Type.levelx;
        }

      
        public ChampionList champions { get; set; }

        #region Serializing

        public int ser_height
        {
            get { return m_height; }
            set
            {
                m_height = value;
                NotifyPropertyChanged("height");
            }
        }

        public int ser_width
        {
            get { return m_width; }
            set
            {
                m_width = value;
                NotifyPropertyChanged("width");
            }
        }

        public int ser_number_of_landmines
        {
            get { return m_number_of_landmines; }
            set
            {
                m_number_of_landmines = value;
                NotifyPropertyChanged("number_of_landmines");
            }
        }

        public bool[] ser_mode
        {
            get { return m_mode; }
            set
            {
                m_mode = value;
                NotifyPropertyChanged("newcomer");
                NotifyPropertyChanged("standard");
                NotifyPropertyChanged("professional");
                NotifyPropertyChanged("custom");
            }
        }

        #endregion

        private int m_height, m_width, m_number_of_landmines;
        private bool[] m_mode;//0 - newcomer, 1 - standard, 2 - professional, 3 - custom game
        private bool m_soundEffects;
    }
}
