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
            cboxSection.Text = preSection;
            ShowSection();
            UpdateCboxExampleItems();
            AttachEvent();
        }

        public void DiscardChanges()
        {
            var config = container.config;

            if (preSection == ConfigDotJson)
            {
                content = config.ToString();
                return;
            }

            var part = Lib.Utils.GetKey(config, preSection);
            if (part != null)
            {
                content = part.ToString();
                return;
            }

            content = sections[preSection];
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

        public void ReloadSection()
        {
            RefreshSections();
            ShowSection();
        }

        public void ShowSection()
        {
            var key = preSection;
            var config = container.config;

            if (!sections.Keys.Contains(key))
            {
                key = ConfigDotJson;
                preSection = key;
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
        bool IsJsonCollection(JToken token)
        {
            if (token == null)
            {
                return false;
            }

            if (token.Type == JTokenType.Object
                  || token.Type == JTokenType.Array)
            {
                return true;
            }

            return false;
        }

        Dictionary<string, string> GetValidSections()
        {
            var config = container.config;
            var defSections = VgcApis.Models.Consts.Config.GetDefCfgSections();

            VgcApis.Libs.Utils
                .GetterJsonSections(config)
                .Where(kv => IsJsonCollection(Lib.Utils.GetKey(config, kv.Key)))
                .ToList()
                .ForEach(kv => defSections[kv.Key] = kv.Value);

            return defSections;
        }

        void RefreshSections()
        {
            var config = container.config;
            sections = GetValidSections();
            RefreshCboxSectionsItems();
        }

        void RefreshCboxSectionsItems()
        {
            var oldText = cboxSection.Text;
            cboxSection.Items.Clear();
            var keys = sections.Keys.OrderBy(k => k).ToList();
            keys.Insert(0, ConfigDotJson);

            foreach (var key in keys)
            {
                cboxSection.Items.Add(key);
            }

            Lib.UI.ResetComboBoxDropdownMenuWidth(cboxSection);
            cboxSection.Text = oldText;
        }

        void OnCboxSectionTextChangedHandler(object sender, EventArgs args)
        {
            cboxSection.TextChanged -= OnCboxSectionTextChangedHandler;

            CboxSectionTextChangedWorker();

            cboxSection.TextChanged += OnCboxSectionTextChangedHandler;
        }

        void CboxSectionTextChangedWorker()
        {
            var text = cboxSection.Text;
            if (text == preSection)
            {
                return;
            }

            if (string.IsNullOrEmpty(text))
            {
                cboxSection.Text = preSection;
                return;
            }

            if (!IsReadyToSwitchSection())
            {
                cboxSection.Text = preSection;
                return;
            }

            RefreshSections();

            preSection = text;
            ShowSection();

            // show section may change preSection;
            cboxSection.Text = preSection;

            UpdateCboxExampleItems();
            container.Update();
        }


        void AttachEvent()
        {
            cboxSection.TextChanged += OnCboxSectionTextChangedHandler;

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
            cboxExample.SelectedIndex = 0;
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

        bool IsReadyToSwitchSection()
        {
            if (CheckValid())
            {
                SaveChanges();
                return true;
            }

            return Lib.UI.Confirm(I18N.CannotParseJson);
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

            var part = Lib.Utils.GetKey(config, preSection);
            if (part != null)
            {
                part.Replace(content);
            }
            else
            {
                var mixin = Lib.Utils.CreateJObject(preSection, content);
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
