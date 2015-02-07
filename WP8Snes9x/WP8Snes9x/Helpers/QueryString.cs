using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace DucLe.Extensions
{
    public class QueryString
    {
        private readonly Dictionary<string, string> _parameters;

        public QueryString()
        {
            _parameters = new Dictionary<string, string>();
        }

        public QueryString(Dictionary<string, string> parameters)
        {
            _parameters = parameters;
        }


        public QueryString(Uri uri) : this(uri.ToString()) { }

        public QueryString(string uri)
            : this()
        {
            Append(uri);
        }

        public void Append(string uri)
        {
            string query;

            if (uri.IndexOf('?') > -1)
                query = uri.Substring(uri.IndexOf('?') + 1);
            else
                return;

            var parts = query.Split('&');
            foreach (var data in parts.Select(s => s.Split('=')))
            {
                _parameters.Add(data[0], HttpUtility.UrlDecode(data[1]));
            }
        }

        public string this[string key]
        {
            get { return (_parameters.ContainsKey(key)) ? _parameters[key] : null; }
            set
            {
                if (_parameters.ContainsKey(key))
                {
                    _parameters[key] = value;
                }
                else
                {
                    _parameters.Add(key, value);
                }
            }
        }

        public bool ContainsKey(string key)
        {
            return _parameters.ContainsKey(key);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var parameter in _parameters)
            {
                if (sb.Length > 0) sb.Append('&');
                sb.AppendFormat("{0}={1}", parameter.Key, parameter.Value);
            }
            return sb.ToString();
        }
    }


}
