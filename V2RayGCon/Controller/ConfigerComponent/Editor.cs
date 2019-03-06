using Newtonsoft.Json.Linq;
using ScintillaNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Controller.ConfigerComponet
{
    class Editor : ConfigerComponentController
    {
        Service.Cache cache;

        Scintilla editor;
        ComboBox cboxSection, cboxExample;
        Button btnFormat, btnRestore;

        bool isDisableCboxSectionChangeEvent = false;

        Dictionary<string, string> sections;
        string preSection = @"";
        string ConfigDotJson = VgcApis.Models.Consts.Config.ConfigDotJson;

        public Editor(
            Panel container,
            ComboBox cboxSection,
            ComboBox cboxExample,
            Button btnFormat,
            Button btnRestore)
        {
            cache = Service.Cache.Instance;

            this.cboxSection = cboxSection;
            this.cboxExample = cboxExample;
            this.btnFormat = btnFormat;
            this.btnRestore = btnRestore;

            BindEditor(container);
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
        public void Prepare()
        {

            preSection = ConfigDotJson;
            RefreshSections();
            ShowSection();
            UpdateCboxExampleItems();
            AttachEvent();
        }

        public void DiscardChanges()
        {
            var config = container.config;

            content =
                preSection == ConfigDotJson ?
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

        public void ShowSection()
        {
            var key = preSection;
            var config = container.config;

            if (!sections.Keys.Contains(key))
            {
                key = ConfigDotJson;
            }

            if (key == ConfigDotJson)
            {
                content = config.ToString();
                return;
            }
            var part = Lib.Utils.GetKey(config, key);
            if (part != null)
            {
                content = part.ToString();
                return;
            }

            var c = sections[key];
            var token = Lib.Utils.CreateJObject(key, JToken.Parse(c));
            Lib.Utils.MergeJson(ref config, token);
            content = c;
            RefreshSections();
        }

        public void ShowEntireConfig()
        {
            this.cboxSection.Text = ConfigDotJson;
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
        void RefreshSections()
        {
            isDisableCboxSectionChangeEvent = true;
            var config = container.config;
            this.sections = VgcApis.Libs.Utils.GenConfigSections(config);
            RefreshCboxSectionsItems();
        }

        void RefreshCboxSectionsItems()
        {
            VgcApis.Libs.UI.RunInUiThread(
                cboxSection, () =>
                {
                    cboxSection.Items.Clear();
                    var keys = sections.Keys.OrderBy(k => k).ToList();
                    keys.Insert(0, ConfigDotJson);

                    foreach (var key in keys)
                    {
                        cboxSection.Items.Add(key);
                    }

                    Lib.UI.ResetComboBoxDropdownMenuWidth(cboxSection);

                    cboxSection.Text = preSection;
                    isDisableCboxSectionChangeEvent = false;
                });
        }

        void AttachEvent()
        {
            cboxSection.TextChanged += (s, e) =>
            {
                if (isDisableCboxSectionChangeEvent)
                {
                    return;
                }

                var text = cboxSection.Text;
                if (text == preSection)
                {
                    return;
                }

                if (string.IsNullOrEmpty(text)
                || !IsSwitchedToNewSection(text))
                {
                    cboxSection.Text = preSection;
                }
                else
                {
                    // update examples
                    cboxSection.Text = preSection;
                    UpdateCboxExampleItems();
                }
            };

            btnFormat.Click += (s, e) =>
            {
                FormatCurrentContent();
            };

            btnRestore.Click += (s, e) =>
            {
                cboxExample.SelectedIndex = 0;
                DiscardChanges();
            };

            cboxExample.SelectedIndexChanged += (s, e) =>
            {
                var index = cboxExample.SelectedIndex - 1;
                if (index < 0)
                {
                    return;
                }
                try
                {
                    this.content = LoadExample(index);
                }
                catch
                {
                    MessageBox.Show(I18N.EditorNoExampleForThisSection);
                }
            };
        }

        void UpdateCboxExampleItems()
        {
            List<string> descriptions = GetExampleItemList();
            cboxExample.Items.Clear();

            cboxExample.Items.Add(I18N.AvailableExamples);
            if (descriptions.Count < 1)
            {
                cboxExample.Enabled = false;
                return;
            }

            cboxExample.Enabled = true;
            foreach (var description in descriptions)
            {
                cboxExample.Items.Add(description);
            }
            Lib.UI.ResetComboBoxDropdownMenuWidth(cboxExample);
            cboxExample.SelectedIndex = 0;
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

        string LoadExample(int index)
        {
            var example = Model.Data.Table.examples[preSection][index];
            switch (preSection)
            {
                case "inbound":
                    return LoadInOutBoundExample(example, true);
                case "outbound":
                    return LoadInOutBoundExample(example, false);
                case "inbounds":
                    return
                        string.Format("[{0}]", LoadInOutBoundExample(example, true));
                case "outbounds":
                    return
                        string.Format("[{0}]", LoadInOutBoundExample(example, false));
                default:
                    return cache.tpl.LoadExample(example[1]).ToString();
            }
        }

        bool IsSwitchedToNewSection(string curSection)
        {
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

        List<string> GetExampleItemList()
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

            if (preSection == ConfigDotJson)
            {
                return config.DeepClone();
            }

            var part = Lib.Utils.GetKey(config, preSection);
            if (part != null)
            {
                return part.DeepClone();
            }
            return JToken.Parse(sections[preSection]);
        }

        void SaveChanges()
        {
            var content = JToken.Parse(this.content);

            if (preSection == ConfigDotJson)
            {
                container.config = content as JObject;
                RefreshSections();
                return;
            }

            var config = container.config;
            var newPart = JToken.Parse(sections[preSection]);

            var part = Lib.Utils.GetKey(config, preSection);
            if (part != null)
            {
                part.Replace(newPart);
            }
            else
            {
                var mixin = Lib.Utils.CreateJObject(preSection, newPart);
                Lib.Utils.MergeJson(ref config, mixin);
            }
            RefreshSections();
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
