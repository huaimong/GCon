using Newtonsoft.Json.Linq;
using ScintillaNET;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Controller.ConfigerComponet
{
    class Editor : ConfigerComponentController
    {
        Service.Cache cache;

        int preSection;
        int separator;
        Scintilla editor;
        ComboBox cboxSection;

        Dictionary<int, string> sections;

        public Editor(
            Panel container,
            ComboBox section,
            ComboBox example,
            Button format,
            Button restore)
        {
            cache = Service.Cache.Instance;

            separator = (int)Model.Data.Enum.Sections.Seperator;
            sections = Model.Data.Table.configSections;
            preSection = 0;

            this.cboxSection = section;
            BindEditor(container);
            AttachEvent(section, example, format, restore);

            Lib.UI.FillComboBox(section, Model.Data.Table.configSections);
        }

        #region properties
        private string _content;

        public string content
        {
            get
            {
                return _content;
            }
            set
            {
                SetField(ref _content, value);
            }
        }
        #endregion

        #region pulbic method
        public void DiscardChanges()
        {
            var config = container.config;

            content =
                preSection == 0 ?
                config.ToString() :
                config[sections[preSection]].ToString();
        }

        public bool IsChanged()
        {
            if (!CheckValid())
            {
                return true;
            }

            var content = JToken.Parse(this.content);
            var section = GetCurConfigSection();

            if (JToken.DeepEquals(content, section))
            {
                return false;
            }

            return true;
        }

        public Scintilla GetEditor()
        {
            if (editor == null)
            {
                throw new NullReferenceException("Editor not ready!");
            }
            return editor;
        }

        public void ShowSection(int section = -1)
        {
            var index = section < 0 ? preSection : section;
            var config = container.config;

            index = Lib.Utils.Clamp(index, 0, sections.Count);

            if (index == 0)
            {
                content = config.ToString();
                return;
            }

            var part = config[sections[index]];
            if (part == null)
            {
                if (index >= separator)
                {
                    part = new JArray();
                }
                else
                {
                    part = new JObject();
                }
                config[sections[index]] = part;
            }
            content = part.ToString();
        }

        public void SelectSection(int index)
        {
            this.cboxSection.SelectedIndex = index;
        }

        public bool Flush()
        {
            if (!CheckValid())
            {
                if (Lib.UI.Confirm(I18N.EditorDiscardChange))
                {
                    DiscardChanges();
                }
                else
                {
                    return false;
                }
            }

            SaveChanges();
            return true;
        }

        public override void Update(JObject config)
        {
            // do nothing
        }
        #endregion

        #region private method
        void AttachEvent(
            ComboBox section,
            ComboBox example,
            Button format,
            Button restore)
        {
            section.SelectedIndexChanged += (s, e) =>
            {
                if (!OnSectionChanged(section.SelectedIndex))
                {
                    section.SelectedIndex = preSection;
                }
                else
                {
                    // update examples
                    UpdateExamplesDescription(example);
                }
            };

            format.Click += (s, e) =>
            {
                FormatCurrentContent();
            };

            restore.Click += (s, e) =>
            {
                example.SelectedIndex = 0;
                DiscardChanges();
            };

            example.SelectedIndexChanged += (s, e) =>
            {
                LoadExample(example.SelectedIndex - 1);
            };
        }

        void UpdateExamplesDescription(ComboBox cboxExamples)
        {
            cboxExamples.Items.Clear();

            cboxExamples.Items.Add(I18N.AvailableExamples);
            var descriptions = GetExamplesDescription();
            if (descriptions.Count < 1)
            {
                cboxExamples.Enabled = false;
            }
            else
            {
                int maxWidth = 0, temp = 0;
                var font = cboxExamples.Font;
                cboxExamples.Enabled = true;
                foreach (var description in descriptions)
                {
                    cboxExamples.Items.Add(description);
                    temp = TextRenderer.MeasureText(description, font).Width;
                    if (temp > maxWidth)
                    {
                        maxWidth = temp;
                    }
                }
                cboxExamples.DropDownWidth = Math.Max(
                    cboxExamples.Width,
                    maxWidth + SystemInformation.VerticalScrollBarWidth);
            }
            cboxExamples.SelectedIndex = 0;
        }

        string LoadInOutBoundExample(string[] example, bool isInbound)
        {
            var tpl = isInbound ?
                cache.tpl.LoadExample("inTpl") :
                cache.tpl.LoadExample("outTpl");

            var protocol = example[2];

            tpl["protocol"] = protocol;
            tpl["settings"] = cache.tpl.LoadExample(example[1]);

            // issue #5
            string[] servProto = { "vmess", "shadowsocks" };
            if (isInbound && Array.IndexOf(servProto, protocol) >= 0)
            {
                tpl["listen"] = "0.0.0.0";
            }

            return tpl.ToString();
        }


        void LoadExample(int index)
        {
            if (index < 0)
            {
                return;
            }

            var example = Model.Data.Table.examples[preSection][index];
            try
            {
                switch (preSection)
                {
                    case (int)Model.Data.Enum.Sections.Inbound:
                        this.content = LoadInOutBoundExample(example, true);
                        break;
                    case (int)Model.Data.Enum.Sections.Outbound:
                        this.content = LoadInOutBoundExample(example, false);
                        break;
                    case (int)Model.Data.Enum.Sections.Inbounds:
                        this.content = string.Format("[{0}]",
                            LoadInOutBoundExample(example, true));
                        break;
                    case (int)Model.Data.Enum.Sections.Outbounds:
                        this.content = string.Format("[{0}]",
                            LoadInOutBoundExample(example, false));
                        break;
                    default:
                        this.content = cache.tpl.LoadExample(example[1]).ToString();
                        break;
                }
            }
            catch
            {
                MessageBox.Show(I18N.EditorNoExample);
            }
        }

        bool OnSectionChanged(int curSection)
        {
            if (curSection == preSection)
            {
                // prevent loop infinitely
                return true;
            }

            if (CheckValid())
            {
                SaveChanges();
                preSection = curSection;
                ShowSection();
                container.Update();
            }
            else
            {
                if (Lib.UI.Confirm(I18N.CannotParseJson))
                {
                    preSection = curSection;
                    ShowSection();
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        List<string> GetExamplesDescription()
        {
            var list = new List<string>();

            var examples = Model.Data.Table.examples;

            if (!examples.ContainsKey(preSection))
            {
                return list;
            }

            foreach (var example in examples[preSection])
            {
                // 0.description 1.keyString
                list.Add(example[0]);
            }

            return list;
        }

        void FormatCurrentContent()
        {
            try
            {
                var json = JToken.Parse(content);
                content = json.ToString();
            }
            catch
            {
                MessageBox.Show(I18N.PleaseCheckConfig);
            }
        }

        JToken GetCurConfigSection()
        {
            var config = container.config;

            JToken section = preSection == 0 ?
                config as JToken :
                config[sections[preSection]];

            return section.DeepClone();
        }

        void SaveChanges()
        {
            var content = JToken.Parse(this.content);

            if (preSection == 0)
            {
                container.config = content as JObject;
                return;
            }

            if (preSection >= separator)
            {
                container.config[sections[preSection]] = content as JArray;
            }
            else
            {
                container.config[sections[preSection]] = content as JObject;
            }
        }

        bool CheckValid()
        {
            try
            {
                JToken.Parse(content);
                return true;
            }
            catch
            {
                return false;
            }
        }

        void BindEditor(Panel container)
        {
            var editor = Lib.UI.CreateScintilla(container);
            this.editor = editor;

            // bind scintilla
            var bs = new BindingSource();
            bs.DataSource = this;
            editor.DataBindings.Add(
                "Text",
                bs,
                nameof(this.content),
                true,
                DataSourceUpdateMode.OnPropertyChanged);
        }
        #endregion
    }
}
