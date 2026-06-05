using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace GlueFramework.Core.IdentityProviders.Saml
{
    public partial class Response
    {
        private static byte[] StringToByteArray(string st)
        {
            byte[] bytes = new byte[st.Length];
            for (int i = 0; i < st.Length; i++)
            {
                bytes[i] = (byte)st[i];
            }
            return bytes;
        }

        protected XmlDocument _xmlDoc;
        protected X509Certificate2 _certificate;
        protected XmlNamespaceManager _xmlNameSpaceManager; //we need this one to run our XPath queries on the SAML XML

        public string Xml { get { return _xmlDoc.OuterXml; } }

        public Response(string responseString)
        {
            LoadXmlFromBase64(responseString);
        }

        public void LoadXml(string xml)
        {
            _xmlDoc = new XmlDocument();
            _xmlDoc.PreserveWhitespace = true;
            _xmlDoc.XmlResolver = null;
            _xmlDoc.LoadXml(xml);

            _xmlNameSpaceManager = GetNamespaceManager(); //lets construct a "manager" for XPath queries
        }

        public void LoadXmlFromBase64(string response)
        {
            UTF8Encoding enc = new UTF8Encoding();
            LoadXml(enc.GetString(Convert.FromBase64String(response)));
        }

        public bool Validate(string certificateStr)
        {
            try
            {
                var certificateBytes = StringToByteArray(certificateStr);
                _certificate = new X509Certificate2(certificateBytes);
            }
            catch (Exception ex)
            {
                throw new Exception("Invalid x509 certificate");
            }

            XmlNodeList nodeList = _xmlDoc.SelectNodes("//ds:Signature", _xmlNameSpaceManager);

            SignedXml signedXml = new SignedXml(_xmlDoc);

            if (nodeList.Count == 0) return false;

            signedXml.LoadXml((XmlElement)nodeList[0]);
            return ValidateSignatureReference(signedXml) && signedXml.CheckSignature(_certificate, true) && !IsExpired();
        }


        private bool ValidateSignatureReference(SignedXml signedXml)
        {
            if (signedXml.SignedInfo.References.Count != 1) //no ref at all
                return false;

            var reference = (Reference)signedXml.SignedInfo.References[0];
            var id = reference.Uri.Substring(1);

            var idElement = signedXml.GetIdElement(_xmlDoc, id);

            if (idElement == _xmlDoc.DocumentElement)
                return true;
            else //sometimes its not the "root" doc-element that is being signed, but the "assertion" element
            {
                var assertionNode = _xmlDoc.SelectSingleNode("/samlp:Response/saml:Assertion", _xmlNameSpaceManager) as XmlElement;
                if (assertionNode != idElement)
                    return false;
            }

            return true;
        }

        private bool IsExpired()
        {
            DateTime expirationDate = DateTime.MaxValue;
            XmlNode node = _xmlDoc.SelectSingleNode("/samlp:Response/saml:Assertion[1]/saml:Subject/saml:SubjectConfirmation/saml:SubjectConfirmationData", _xmlNameSpaceManager);
            if (node != null && node.Attributes["NotOnOrAfter"] != null)
            {
                DateTime.TryParse(node.Attributes["NotOnOrAfter"].Value, out expirationDate);
            }
            return DateTime.UtcNow > expirationDate.ToUniversalTime();
        }

        public string GetIssuer()
        {
            XmlNode node = _xmlDoc.SelectSingleNode("/saml2p:Response/saml2:Issuer", _xmlNameSpaceManager);
            return node == null ? "" : node.InnerText;
        }

        public bool IsSuccessfulResponse()
        {
            var statusNode = _xmlDoc.SelectSingleNode("/samlp:Response/saml2p:Status/saml2p:StatusCode", _xmlNameSpaceManager);
            return statusNode == null ? false : statusNode.Attributes["Value"].Value == "urn:oasis:names:tc:SAML:2.0:status:Success";
        }

        public string GetNameID()
        {
            XmlNode node = _xmlDoc.SelectSingleNode("/samlp:Response/saml:Assertion[1]/saml:Subject/saml:NameID", _xmlNameSpaceManager);
            return node == null ? "" : node.InnerText;
        }


        //returns namespace manager, we need one b/c MS says so... Otherwise XPath doesnt work in an XML doc with namespaces
        //see https://stackoverflow.com/questions/7178111/why-is-xmlnamespacemanager-necessary
        private XmlNamespaceManager GetNamespaceManager()
        {
            XmlNamespaceManager manager = new XmlNamespaceManager(_xmlDoc.NameTable);
            manager.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);
            manager.AddNamespace("saml", "urn:oasis:names:tc:SAML:2.0:assertion");
            manager.AddNamespace("saml2", "urn:oasis:names:tc:SAML:2.0:assertion");
            manager.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");
            manager.AddNamespace("saml2p", "urn:oasis:names:tc:SAML:2.0:protocol");
            return manager;
        }
    }
}
