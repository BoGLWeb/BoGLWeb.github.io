using Newtonsoft.Json;

namespace BoGLWeb.Json {
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Bond {
        public Source source {
            get;
            set;
        }

        public Target target {
            get;
            set;
        }

        public int velocity {
            get;
            set;
        }
    }

    public class Element {
        public int id {
            get;
            set;
        }

        public double x {
            get;
            set;
        }

        public double y {
            get;
            set;
        }

        public List<object> modifiers {
            get;
            set;
        }

        public int velocity {
            get;
            set;
        }

        public int type {
            get;
            set;
        }
    }

    public class Root {
        public List<Element> elements {
            get;
            set;
        }

        public List<Bond> bonds {
            get;
            set;
        }

        public string generateUrl() {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class Source {
        public int id {
            get;
            set;
        }

        public double x {
            get;
            set;
        }

        public double y {
            get;
            set;
        }

        public List<object> modifiers {
            get;
            set;
        }

        public int velocity {
            get;
            set;
        }

        public int type {
            get;
            set;
        }
    }

    public class Target {
        public int id {
            get;
            set;
        }

        public double x {
            get;
            set;
        }

        public double y {
            get;
            set;
        }

        public List<object> modifiers {
            get;
            set;
        }

        public int velocity {
            get;
            set;
        }

        public int type {
            get;
            set;
        }
    }
}