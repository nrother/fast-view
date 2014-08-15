using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Windows.Forms;
using System.Xml;
using System.Collections.Specialized;
using Microsoft.Win32;


namespace GaliNeo.Framework.GaliNeoSettings
{
    class GaliNeoSettingsProvider : SettingsProvider
    {
        XmlDocument doc = null;
        XmlNode root = null;
        XmlNode userSettings = null;
        XmlNode appSettings = null;

        public GaliNeoSettingsProvider()
        {
            doc = new XmlDocument();
            LoadSettings();
        }

        public override string ApplicationName
        {
            get { return Application.ProductName; }
            set { }
        }

        public override void Initialize(string name, NameValueCollection col)
        {
            base.Initialize(this.ApplicationName, col);
        }


        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection propvals)
        {

            foreach (SettingsPropertyValue propval in propvals)
            {

                SettingsProperty prop = propval.Property;

                setValue(prop, propval, context);

            }

            this.WriteSettings();
        }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection props)
        {

            // Create new collection of values
            SettingsPropertyValueCollection values = new SettingsPropertyValueCollection();
            string sectionName = this.GetSectionName(context);
            string settingKey = (string)context["SettingsKey"];


            // Iterate through the settings to be retrieved
            foreach (SettingsProperty setting in props)
            {
                SettingsPropertyValue value = new SettingsPropertyValue(setting);
                value.IsDirty = false;
                value.SerializedValue = getValue(setting, context);

                values.Add(value);
            }
            return values;

        }


        private XmlNode SerializeToXmlElement(SettingsProperty setting, SettingsPropertyValue value)
        {

            XmlElement element = doc.CreateElement("value");
            string text1 = value.SerializedValue as string;
            if ((text1 == null) && (setting.SerializeAs == SettingsSerializeAs.Binary))
            {
                byte[] buffer1 = value.SerializedValue as byte[];
                if (buffer1 != null)
                {
                    text1 = Convert.ToBase64String(buffer1);
                }
            }
            if (text1 == null)
            {
                text1 = string.Empty;
            }
            if (setting.SerializeAs == SettingsSerializeAs.String)
            {
                //text1 = this.Escaper.Escape(text1);
            }

            element.InnerXml = text1;
            XmlNode node1 = null;
            foreach (XmlNode node2 in element.ChildNodes)
            {
                if (node2.NodeType == XmlNodeType.XmlDeclaration)
                {
                    node1 = node2;
                    break;
                }
            }
            if (node1 != null)
            {
                element.RemoveChild(node1);
            }
            return element;
        }

        private string GetSectionName(SettingsContext context)
        {
            string groupName = (string)context["GroupName"];
            string settingKey = (string)context["SettingsKey"];
            string section = groupName;
            if (!string.IsNullOrEmpty(settingKey))
            {
                object[] arrName = new object[] { section, settingKey };
                section = string.Format("{0}.{1}", arrName);
            }
            return XmlConvert.EncodeLocalName(section);
        }


        #region DAN


        private string getXPathQueryProperty(SettingsProperty setting, SettingsContext context)
        {
            string xPathQuery = getXPathQuerySection(setting, context);

            if (setting.Name != "")
            {
                xPathQuery += "/" + setting.Name;
            } //if (propName != "")

            return xPathQuery;
        }

        private string getXPathQuerySection(SettingsProperty setting, SettingsContext context)
        {
            string xPathQueryGroup = doc.DocumentElement.LocalName;

            if (this.IsUserScoped(setting))
            {
                xPathQueryGroup += "/userSettings";
            }
            else
            {
                xPathQueryGroup += "/appSettings";
            }
            xPathQueryGroup += "/" + GetSectionName(context);


            return xPathQueryGroup;

        }



        private void CreatePropNode(SettingsProperty setting, SettingsPropertyValue value, SettingsContext context)
        {
            string xPathQuery = getXPathQuerySection(setting, context);

            XmlNode groupNode = doc.SelectSingleNode(xPathQuery);

            if (groupNode == null)
            {
                groupNode = doc.CreateElement(GetSectionName(context));

                if (this.IsUserScoped(setting))
                {
                    userSettings.AppendChild(groupNode);
                }
                else
                {
                    appSettings.AppendChild(groupNode);
                }

            } //if (node == null)


            XmlNode nodeProp = doc.CreateElement(setting.Name);

            nodeProp.AppendChild(this.SerializeToXmlElement(setting, value));

            groupNode.AppendChild(nodeProp);

        }

        private void setValue(SettingsProperty setting, SettingsPropertyValue value, SettingsContext context) //string propName, string groupName, object value, object defaultvalue)
        {

            string xPathQuery = getXPathQueryProperty(setting, context);

            XmlNode node = doc.SelectSingleNode(xPathQuery);
            if (node != null)
            {
                XmlNode valueNode = SerializeToXmlElement(setting, value);
                node.RemoveAll();
                node.AppendChild(valueNode);
            }
            else
            {

                CreatePropNode(setting, value, context);

            } //if (node != null)

        }

        private object getValue(SettingsProperty setting, SettingsContext context)
        {
            string xPathQuery = getXPathQueryProperty(setting, context);

            XmlNode node = doc.SelectSingleNode(xPathQuery + "/value");

            if (node != null)
            {
                return node.InnerXml;
            }
            else if (setting.DefaultValue != null)
            {
                return setting.DefaultValue;
            }
            else
            {
                return null;
            }

        }

        // Helper method: walks the "attribute bag" for a given property
        // to determine if it is user-scoped or not.
        // Note that this provider does not enforce other rules, such as
        // - unknown attributes
        // - improper attribute combinations (e.g. both user and app - this implementation
        // would say true for user-scoped regardless of existence of app-scoped)
        private bool IsUserScoped(SettingsProperty prop)
        {
            foreach (DictionaryEntry d in prop.Attributes)
            {
                Attribute a = (Attribute)d.Value;
                if (a.GetType() == typeof(UserScopedSettingAttribute))
                    return true;
            }
            return false;
        }

        #endregion

        private void LoadSettings()
        {
            string configFile = getPath() + "\\user.config";

            if (System.IO.File.Exists(configFile))
            {
                doc.Load(configFile);
                root = doc.DocumentElement;
                userSettings = doc.DocumentElement.SelectSingleNode("./userSettings");
                appSettings = doc.DocumentElement.SelectSingleNode("./appSettings"); ;

            }
            else
            {

                try
                {
                    root = doc.CreateElement("configuration");

                    userSettings = doc.CreateElement("userSettings");
                    root.AppendChild(userSettings);
                    appSettings = doc.CreateElement("appSettings");
                    root.AppendChild(appSettings);

                    doc.AppendChild(root);
                }
                catch
                {

                }
            }

        }

        private string getPath()
        {
            return Application.StartupPath;
        }


        private void WriteSettings()
        {
            if (!(System.IO.Directory.Exists(getPath())))
            {
                System.IO.Directory.CreateDirectory(getPath());
            }
            string configFile = getPath() + "\\user.config";


            doc.Save(configFile);


        }

    }
}