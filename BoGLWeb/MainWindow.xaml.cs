using GraphSynth.Representation;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace AVL_Prototype_1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    // todo - make this list more complete
    public enum ModifierType { VELOCITY, FRICTION, PARALLEL, INERTIA, TOOTH_WEAR, DAMPING, STIFFNESS, MASS };

    public partial class MainWindow : Window
    {
        public static Dictionary<String, ImageSource> imageSources = null;
        public static Dictionary<String, List<ModifierType>> elementModifiers = null;
        public static Dictionary<String, int> maxConnections = null;
        public static Dictionary<String, List<String>> elementCompatibility = null;

        #region public variables for rulesets and graphsynth related graphs -can be modified

        public ruleSet systemToBondGraph = null;
        public ruleSet directionRuleSet = null;
        public ruleSet directionRuleSet2 = null;
        public ruleSet directionRuleSet3 = null;
        public ruleSet SimplificationRuleset = null;
        public ruleSet SimplificationRuleset2 = null;
        public ruleSet NewCausalityMethodRuleset = null;
        public ruleSet NewCausalityMethodRuleset_2 = null;
        public ruleSet NewCausalityMethodRuleset_3 = null;
        public ruleSet INVDMarkerRules = null;
        public ruleSet INVDMarkerRules_2 = null;
        public ruleSet CalibrationNewRuleset = null;
        public ruleSet CalibrationNewRuleset_2 = null;
        public ruleSet RFlagCleanRuleset = null;
        public ruleSet ICFixTotalRuleset = null;
        public ruleSet TransformerFlipRuleset = null;
        public ruleSet TransformerFlipRuleset2 = null;
        public ruleSet Clean23Ruleset = null;

        public ruleSet SolidworksRuleset = null;

        public ruleSet State_Ruleset_1 = null;
        public ruleSet State_Ruleset_2 = null;
        public ruleSet State_Ruleset_3 = null;
        public ruleSet State_FormatGraph = null;
        public ruleSet State_Sum_Remove = null;
        public ruleSet State_FormatForSum = null;
        public ruleSet State_Sum_AddLabels = null;
        public List<string> flowEqns = new List<string> { };
        public List<string> effortEqns = new List<string> { };


        public List<designGraph> optiGraphs = new List<designGraph>();

        public List<int> indiceswithoutINVD = new List<int>();
        public List<int> maxIntegralCausality = new List<int>();
        public List<designGraph> finalresult = new List<designGraph>();

        public HashSet<string> nodeLabelSorted = new HashSet<string>();
        public HashSet<int> sortedIndices = new HashSet<int>();

        public List<designGraph> sys_Graphs = new List<designGraph>();

        int applycausality = 0;

        int selectedOption = -1;

        StreamReader ruleReader = null;
        public designGraph systemGraph = null;
        public designGraph unSimplifiedGraph = null;
        public designGraph SimplifiedGraphWithDir = null;
        public designGraph systemGraph_User = null;

        public List<option> options = new List<option>();

        public ObservableCollection<RectangleViewModel> rectangles_BG = new ObservableCollection<RectangleViewModel>();
        public ObservableCollection<LineConnections> lines_BG = new ObservableCollection<LineConnections>();
        public ObservableCollection<ConnectionViewModel> connections_BG = new ObservableCollection<ConnectionViewModel>();

        public ObservableCollection<RectangleViewModel> rectangles_BG_Simplified = new ObservableCollection<RectangleViewModel>();
        public ObservableCollection<LineConnections> lines_BG_Simplified = new ObservableCollection<LineConnections>();
        public ObservableCollection<ConnectionViewModel> connections_BG_Simplified = new ObservableCollection<ConnectionViewModel>();

        public ObservableCollection<RectangleViewModel> rectangles_BG_Causality = new ObservableCollection<RectangleViewModel>();
        public ObservableCollection<LineConnections> lines_BG_Causality = new ObservableCollection<LineConnections>();
        public ObservableCollection<ConnectionViewModel> connections_BG_Causality = new ObservableCollection<ConnectionViewModel>();

        public int index1 = 0;

        double mouseVerticalPosition;
        double mouseHorizontalPosition;

        bool isMouseCaptured = false;
        Point mousePoint;

        bool causality = false;
        bool beforeSim = false;
        bool simplified = false;

        bool mechanicalTrans = false;
        bool mechanicalRot = false;
        bool electrical = false;
        bool mechGround = false;
        bool elecGround = false;

        #endregion 

        // Singleton instance
        private static MainWindow instance;

        // Save and load stuff
        private static bool _unSaved;
        public static bool unSaved
        {
            get => _unSaved;
            set
            {
                _unSaved = value;
                instance.unsavedIndicator.Visibility = _unSaved ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public static string savePath = null;

        public MainWindow()
        {
            InitializeComponent();

            //loading all graph grammar rulesets and rules
            loadrulesets();

            // Remove ALL of the gray dashed lines around clicked elements
            FocusVisualStyleRemover.Init();

            // For maximizing and restoring
            StateChanged += MainWindowStateChangeRaised;
            // Set singleton instance
            instance = this;

            unSaved = false;

            // Define the element modifiers
            defineElementModifiers();
            defineMaxConnections();
            defineElementCompatibility();

            // Load all image sources
            imageSources = new Dictionary<String, ImageSource>();
            BitmapImage bitmap;

            foreach (Expander e in elementsStack.Children)
            {
                WrapPanel wp = (WrapPanel)e.Content;

                foreach (ElementTemplate et in wp.Children)
                {
                    //et.toolTup = elementNiceNAmes [et.Name] ;
                    if (et.Name.Contains("System_MT_"))
                    {
                        bitmap = new BitmapImage();
                        String eltName = et.Name.Substring(10);
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri("pack://application:,,,/icons/mech translation/" + eltName + ".png");
                        bitmap.EndInit();
                        et.ToolTip = eltName.Replace("_", " ");
                        et.iconImage.Source = bitmap;
                        imageSources[et.Name] = bitmap;
                    }
                    else if (et.Name.Contains("System_MR_"))
                    {
                        bitmap = new BitmapImage();
                        String eltName = et.Name.Substring(10);
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri("pack://application:,,,/icons/mech rotation/" + eltName + ".png");
                        bitmap.EndInit();
                        et.ToolTip = eltName.Replace("_", " ");
                        et.iconImage.Source = bitmap;
                        imageSources[et.Name] = bitmap;
                    }
                    else if (et.Name.Contains("System_E_"))
                    {
                        bitmap = new BitmapImage();
                        String eltName = et.Name.Substring(9);
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri("pack://application:,,,/icons/electrical/" + (eltName == "Junction" ? "junction_palette" : eltName) + ".png");
                        bitmap.EndInit();
                        et.ToolTip = eltName.Replace("_", " ");
                        et.iconImage.Source = bitmap;
                        imageSources[et.Name == "System_E_Junction" ? "System_E_Junction_Palette" : et.Name] = bitmap;
                    }
                    else if (et.Name.Contains("System_O_"))
                    {
                        bitmap = new BitmapImage();
                        String eltName = et.Name.Substring(9);
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri("pack://application:,,,/icons/other/" + eltName + ".png");
                        bitmap.EndInit();
                        et.ToolTip = eltName.Replace("_", " ");
                        et.iconImage.Source = bitmap;
                        imageSources[et.Name] = bitmap;
                    }
                }
            }

            // Special cases for images

            // Load the image for a junction when it is small
            bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri("pack://application:,,,/icons/electrical/junction.png");
            bitmap.EndInit();
            imageSources["System_E_Junction"] = bitmap;

            // Disable generate button and modifier lists on Bond Graphs
            List<Graph> graphs = new List<Graph>();
            graphs.Add(Graph_BG1);
            graphs.Add(Graph_BG2);
            graphs.Add(Graph_BG3);

            graphs.ForEach(graph =>
            {
                graph.generateButton.IsEnabled = false;
                graph.modifierList.IsEnabled = false;
                graph.modifierList.Visibility = Visibility.Collapsed;
                graph.modExpander.IsExpanded = false;

                graph.bigCanvas.AllowDrop = false;
            });

            graphs.Remove(Graph_BG3);
            graphs.Add(Graph_System);

            graphs.ForEach(graph =>
            {
                graph.CausalOptions.IsEnabled = false;
                graph.CausalOptions.Visibility = Visibility.Collapsed;
            });
            Graph_BG3.CausalOptions.IsEnabled = false;
        }

        #region rules, rulesets related to bond graph generation
        protected object[] OpenRuleAndCanvas(string filename)
        {
           // XmlReader xR = null;
            //var assembly = Assembly.GetExecutingAssembly();
            //var filename = "AVL_Prototype_1.Rules.BondGraphRuleset.rsxml";
           // Stream stream = assembly.GetManifestResourceStream(filename);
           // StreamReader reader = new StreamReader(stream);
           // string text = reader.ReadToEnd();

            Uri uri = new Uri(filename, UriKind.Relative);
            System.Windows.Resources.StreamResourceInfo info = Application.GetResourceStream(uri);
           StreamReader reader = new StreamReader(info.Stream);
            string text = reader.ReadToEnd();

            //xR = XmlReader.Create(filename);
            // var xeRule = XElement.Load(stream);

            var xeRule = XElement.Parse(text);

            var temp = xeRule.Element("{ignorableUri}" + "grammarRule");
            var openRule = new grammarRule();
            if (temp != null)
                openRule = DeSerializeRuleFromXML(RemoveXAMLns(RemoveIgnorablePrefix(temp.ToString())));

            removeNullWhiteSpaceEmptyLabels(openRule.L);
            removeNullWhiteSpaceEmptyLabels(openRule.R);
          //  xR.Dispose();
          //  xR.Close();


            return new object[] { openRule, filename };


        }
        protected grammarRule DeSerializeRuleFromXML(string xmlString)
        {

            var stringReader = new StringReader(xmlString);
            var ruleDeserializer = new XmlSerializer(typeof(grammarRule));
            var newGrammarRule = (grammarRule)ruleDeserializer.Deserialize(stringReader);
            if (newGrammarRule.L == null) newGrammarRule.L = new designGraph();
            else newGrammarRule.L.internallyConnectGraph();

            if (newGrammarRule.R == null) newGrammarRule.R = new designGraph();
            else newGrammarRule.R.internallyConnectGraph();

            return newGrammarRule;


        }
        public const string IgnorablePrefix = "GraphSynth:";
        public string RemoveXAMLns(string s)
        {
            //get rid of all the xaml related namespace stuff 
            // how to do this without hardcoding?
            // -- k spent a lot of time to know that it had to be removed for successful deserialization oofff!
            s = s.Replace("xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"", "");

            return s;
        }
        public string RemoveIgnorablePrefix(string x)
        {
            return x.Replace(IgnorablePrefix, "").Replace("xmlns=\"ignorableUri\"", "");
        }
        public static void removeNullWhiteSpaceEmptyLabels(designGraph g)
        {
            g.globalLabels.RemoveAll(string.IsNullOrWhiteSpace);
            foreach (var a in g.arcs)
                a.localLabels.RemoveAll(string.IsNullOrWhiteSpace);
            foreach (var a in g.nodes)
                a.localLabels.RemoveAll(string.IsNullOrWhiteSpace);
            foreach (var a in g.hyperarcs)
                a.localLabels.RemoveAll(string.IsNullOrWhiteSpace);
        }
        protected List<grammarRule> LoadRulesFromFileNames(string ruleDir, List<string> ruleFileNames,
                                                                    out int numLoaded)
        {
            var progStart = 5;
            var progStep = (double)(100 - progStart) / ruleFileNames.Count;
            //var step = 0;
            var rules = new List<grammarRule>();
            numLoaded = 0;
            while (numLoaded < ruleFileNames.Count)
            {
                // var rulePath = ruleDir + ruleFileNames[numLoaded];
                // var rulePath = "AVL_Prototype_1.Rules."+ruleFileNames[numLoaded];
                //var filename = new Uri("pack://application:,,,/Rules/BondGraphRuleset.rsxml");
                // var rulePath = new Uri("pack://application:,,,/Rules/" + ruleFileNames[numLoaded]).LocalPath;

                var rulePath = "/Rules/" + ruleFileNames[numLoaded];

                //  var rulename1 = ruleFileNames[numLoaded];

                //  ResourceManager rm = new ResourceManager("FileStore.Resource1",Assembly.GetExecutingAssembly());
                //   string filename = rm.GetString(rulename1); 

                //  if (File.Exists(rulePath))

                {

                    object ruleObj = OpenRuleAndCanvas(rulePath);
                    if (ruleObj is grammarRule)
                        rules.Add((grammarRule)ruleObj);
                    else if (ruleObj is object[])
                        foreach (object o in (object[])ruleObj)
                            if (o is grammarRule)
                                rules.Add((grammarRule)o);
                    numLoaded++;
                }
              /*  else
                {

                    ruleFileNames.RemoveAt(numLoaded);
                } */

            }
            return rules;
        }
        private void loadrulesets()
        {
            //  WebClient Client = new WebClient();
            //  Client.DownloadFile("http://users.wpi.edu/~pradhakrishnan/Rules.zip", AppDomain.CurrentDomain.BaseDirectory + "Rules.zip");


            //extract file
            //  string filepath = string.Concat(AppDomain.CurrentDomain.BaseDirectory.ToString(), "Rules.zip");

            //   string startPath = AppDomain.CurrentDomain.BaseDirectory;
            //   string zipPath = filepath;
            //  string extractPath = AppDomain.CurrentDomain.BaseDirectory + "Rules";

            // System.IO.Compression.ZipFile.CreateFromDirectory(startPath, zipPath);
            //  System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);
            //   "c:\\  
            
            #region RuleSet - 0 - System to BondGraph

            // var filename = "Rules\\BondGraphRuleset.rsxml";
            //var filename = new Uri("pack://application:,,,/Rules/BondGraphRuleset.rsxml");
            //  var filename = FileStore.Resource1.BondGraphRuleset.ToString();
            //System.Windows.Resources.StreamResourceInfo info = Application.GetResourceStream(filename);
            //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
            //var filename = Encoding.ASCII.GetString(FileStore.Resource1.BondGraphRuleset);
            // Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(filename));
            // ruleReader = new StreamReader(info.Stream)

           
            //  var assembly = Assembly.GetExecutingAssembly();
            //  var filename = "AVL_Prototype_1.Rules.BondGraphRuleset.rsxml";
            //  Stream stream = assembly.GetManifestResourceStream(filename);
            Uri uri = new Uri("/Rules/BondGraphRuleset.rsxml", UriKind.Relative);
            System.Windows.Resources.StreamResourceInfo info = Application.GetResourceStream(uri);
            ruleReader = new StreamReader(info.Stream);

            //ruleReader = new StreamReader(stream);
           
            var ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            systemToBondGraph = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
            //systemToBondGraph.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";
           // systemToBondGraph.rulesDir = System.IO.Path.GetDirectoryName("Rules\\");
            int numLoaded;
            var numRules = systemToBondGraph.ruleFileNames.Count;
            systemToBondGraph.rules = LoadRulesFromFileNames(systemToBondGraph.rulesDir, systemToBondGraph.ruleFileNames, out numLoaded);
            ruleReader.Dispose();

            foreach (var item in systemToBondGraph.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            #endregion

            #region RuleSet - 1 - Simplification RuleSet
            //filename = "Rules\\SimplificationRuleset.rsxml";
            //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
            // ruleReader = new StreamReader(filename);

             uri = new Uri("/Rules/SimplificationRuleset.rsxml", UriKind.Relative);
            info = Application.GetResourceStream(uri);
            ruleReader = new StreamReader(info.Stream);

            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            SimplificationRuleset = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
           // SimplificationRuleset.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = SimplificationRuleset.ruleFileNames.Count;
            SimplificationRuleset.rules = LoadRulesFromFileNames(SimplificationRuleset.rulesDir, SimplificationRuleset.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in SimplificationRuleset.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            #endregion

            #region RuleSet - 2 Directions
            /* filename = "Rules\\DirRuleset.rsxml";
             //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
             ruleReader = new StreamReader(filename); */

            // assembly = Assembly.GetExecutingAssembly();
            // filename = "AVL_Prototype_1.Rules.DirRuleset.rsxml";
            // stream = assembly.GetManifestResourceStream(filename);

            //  ruleReader = new StreamReader(stream);

             uri = new Uri("/Rules/DirRuleset.rsxml", UriKind.Relative);
            info = Application.GetResourceStream(uri);
            ruleReader = new StreamReader(info.Stream);

            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            directionRuleSet = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
           // directionRuleSet.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            

            numRules = directionRuleSet.ruleFileNames.Count;
            directionRuleSet.rules = LoadRulesFromFileNames(directionRuleSet.rulesDir, directionRuleSet.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in directionRuleSet.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            /* filename = "Rules\\newDirectionRuleSet_2.rsxml";
             //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
             ruleReader = new StreamReader(filename); */
            // assembly = Assembly.GetExecutingAssembly();
            //filename = "AVL_Prototype_1.Rules.newDirectionRuleSet_2.rsxml";
            //stream = assembly.GetManifestResourceStream(filename);

            //  ruleReader = new StreamReader(stream);

             uri = new Uri("/Rules/newDirectionRuleSet_2.rsxml", UriKind.Relative);
            info = Application.GetResourceStream(uri);
            ruleReader = new StreamReader(info.Stream);


            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            directionRuleSet2 = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
            //directionRuleSet2.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = directionRuleSet2.ruleFileNames.Count;
            directionRuleSet2.rules = LoadRulesFromFileNames(directionRuleSet2.rulesDir, directionRuleSet2.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in directionRuleSet2.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }


            /* filename = "Rules\\DirRuleset3.rsxml";
             //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
             ruleReader = new StreamReader(filename); */

            /*   assembly = Assembly.GetExecutingAssembly();
               filename = "AVL_Prototype_1.Rules.DirRuleset3.rsxml";
               stream = assembly.GetManifestResourceStream(filename);
               ruleReader = new StreamReader(stream); */

             uri = new Uri("/Rules/DirRuleset3.rsxml", UriKind.Relative);
            info = Application.GetResourceStream(uri);
            ruleReader = new StreamReader(info.Stream);

            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            directionRuleSet3 = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
           // directionRuleSet3.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = directionRuleSet3.ruleFileNames.Count;
            directionRuleSet3.rules = LoadRulesFromFileNames(directionRuleSet3.rulesDir, directionRuleSet3.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in directionRuleSet3.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }


            #endregion

            #region RuleSet - 3 - Simplification RuleSet 2
            /* filename = "Rules\\Simplification2.rsxml";
             //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
             ruleReader = new StreamReader(filename); */
            /*  assembly = Assembly.GetExecutingAssembly();
              filename = "AVL_Prototype_1.Rules.Simplification2.rsxml";
              stream = assembly.GetManifestResourceStream(filename);
              ruleReader = new StreamReader(stream); */

             uri = new Uri("/Rules/Simplification2.rsxml", UriKind.Relative);
            info = Application.GetResourceStream(uri);
            ruleReader = new StreamReader(info.Stream);

            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            SimplificationRuleset2 = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
          //  SimplificationRuleset2.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = SimplificationRuleset2.ruleFileNames.Count;
            SimplificationRuleset2.rules = LoadRulesFromFileNames(SimplificationRuleset2.rulesDir, SimplificationRuleset2.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in SimplificationRuleset2.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            #endregion

            #region RuleSet - 4: Causality Method
            /*   filename = "Rules\\NewCausalityMethodRuleset.rsxml";
               //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
               ruleReader = new StreamReader(filename); */

            /* assembly = Assembly.GetExecutingAssembly();
             filename = "AVL_Prototype_1.Rules.NewCausalityMethodRuleset.rsxml";
             stream = assembly.GetManifestResourceStream(filename);
             ruleReader = new StreamReader(stream);*/

             uri = new Uri("/Rules/NewCausalityMethodRuleset.rsxml", UriKind.Relative);
             info = Application.GetResourceStream(uri);
            ruleReader = new StreamReader(info.Stream);

            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            NewCausalityMethodRuleset = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
         //   NewCausalityMethodRuleset.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = NewCausalityMethodRuleset.ruleFileNames.Count;
            NewCausalityMethodRuleset.rules = LoadRulesFromFileNames(NewCausalityMethodRuleset.rulesDir, NewCausalityMethodRuleset.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in NewCausalityMethodRuleset.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }


            /* filename = "Rules\\NewCausalityMethodRuleset_2.rsxml";
             //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
             ruleReader = new StreamReader(filename); */

            /*    assembly = Assembly.GetExecutingAssembly();
                filename = "AVL_Prototype_1.Rules.NewCausalityMethodRuleset_2.rsxml";
                stream = assembly.GetManifestResourceStream(filename);
                ruleReader = new StreamReader(stream); */

             uri = new Uri("/Rules/NewCausalityMethodRuleset_2.rsxml", UriKind.Relative);
           info = Application.GetResourceStream(uri);
            ruleReader = new StreamReader(info.Stream);

            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            NewCausalityMethodRuleset_2 = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
           // NewCausalityMethodRuleset_2.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = NewCausalityMethodRuleset_2.ruleFileNames.Count;
            NewCausalityMethodRuleset_2.rules = LoadRulesFromFileNames(NewCausalityMethodRuleset_2.rulesDir, NewCausalityMethodRuleset_2.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in NewCausalityMethodRuleset_2.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            /* filename = "Rules\\NewCausalityMethodRuleset_3.rsxml";
             //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
             ruleReader = new StreamReader(filename); */

            /*   assembly = Assembly.GetExecutingAssembly();
               filename = "AVL_Prototype_1.Rules.NewCausalityMethodRuleset_3.rsxml";
               stream = assembly.GetManifestResourceStream(filename);
               ruleReader = new StreamReader(stream); */

             uri = new Uri("/Rules/NewCausalityMethodRuleset_3.rsxml", UriKind.Relative);
            info = Application.GetResourceStream(uri);
            ruleReader = new StreamReader(info.Stream);

            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            NewCausalityMethodRuleset_3 = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
           // NewCausalityMethodRuleset_3.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = NewCausalityMethodRuleset_3.ruleFileNames.Count;
            NewCausalityMethodRuleset_3.rules = LoadRulesFromFileNames(NewCausalityMethodRuleset_3.rulesDir, NewCausalityMethodRuleset_3.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in NewCausalityMethodRuleset_3.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            #endregion

            #region RuleSet - 5 - INVDMarkerRules
            /*filename = "Rules\\INVDMarkerRules.rsxml";
            //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
            ruleReader = new StreamReader(filename);*/

            /*   assembly = Assembly.GetExecutingAssembly();
               filename = "AVL_Prototype_1.Rules.INVDMarkerRules.rsxml";
               stream = assembly.GetManifestResourceStream(filename);
               ruleReader = new StreamReader(stream);*/

             uri = new Uri("/Rules/INVDMarkerRules.rsxml", UriKind.Relative);
            info = Application.GetResourceStream(uri);
            ruleReader = new StreamReader(info.Stream);

            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            INVDMarkerRules = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
           // INVDMarkerRules.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = INVDMarkerRules.ruleFileNames.Count;
            INVDMarkerRules.rules = LoadRulesFromFileNames(INVDMarkerRules.rulesDir, INVDMarkerRules.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in INVDMarkerRules.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            //  filename = "Rules\\INVDMarkerRules_2.rsxml";
            //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
            // ruleReader = new StreamReader(filename);

            uri = new Uri("/Rules/INVDMarkerRules_2.rsxml", UriKind.Relative);
            info = Application.GetResourceStream(uri);
            ruleReader = new StreamReader(info.Stream);

            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            INVDMarkerRules_2 = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
           // INVDMarkerRules_2.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = INVDMarkerRules_2.ruleFileNames.Count;
            INVDMarkerRules_2.rules = LoadRulesFromFileNames(INVDMarkerRules_2.rulesDir, INVDMarkerRules_2.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in INVDMarkerRules_2.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            #endregion

            #region RuleSet - 6 - Calibration New Ruleset
            /* filename = "Rules\\CalibrationNewRuleset.rsxml";
             //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
             ruleReader = new StreamReader(filename); */

            /*   assembly = Assembly.GetExecutingAssembly();
               filename = "AVL_Prototype_1.Rules.CalibrationNewRuleset.rsxml";
               stream = assembly.GetManifestResourceStream(filename);
               ruleReader = new StreamReader(stream);*/

            uri = new Uri("/Rules/CalibrationNewRuleset.rsxml", UriKind.Relative);
            info = Application.GetResourceStream(uri);
            ruleReader = new StreamReader(info.Stream);

            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            CalibrationNewRuleset = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
          //  CalibrationNewRuleset.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = CalibrationNewRuleset.ruleFileNames.Count;
            CalibrationNewRuleset.rules = LoadRulesFromFileNames(CalibrationNewRuleset.rulesDir, CalibrationNewRuleset.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in CalibrationNewRuleset.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            /* filename = "Rules\\CalibrationNewRuleset_2.rsxml";
             //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
             ruleReader = new StreamReader(filename); */

            /*    assembly = Assembly.GetExecutingAssembly();
                filename = "AVL_Prototype_1.Rules.CalibrationNewRuleset_2.rsxml";
                stream = assembly.GetManifestResourceStream(filename);
                ruleReader = new StreamReader(stream);*/

            uri = new Uri("/Rules/CalibrationNewRuleset_2.rsxml", UriKind.Relative);
            info = Application.GetResourceStream(uri);
            ruleReader = new StreamReader(info.Stream);

            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            CalibrationNewRuleset_2 = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
           // CalibrationNewRuleset_2.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = CalibrationNewRuleset_2.ruleFileNames.Count;
            CalibrationNewRuleset_2.rules = LoadRulesFromFileNames(CalibrationNewRuleset_2.rulesDir, CalibrationNewRuleset_2.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in CalibrationNewRuleset_2.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }



            #endregion

            #region RuleSet - 7 - RFlagCleanRuleset
            /*filename = "Rules\\RFlagCleanRuleset.rsxml";
            //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
            ruleReader = new StreamReader(filename);*/

            /*   assembly = Assembly.GetExecutingAssembly();
               filename = "AVL_Prototype_1.Rules.RFlagCleanRuleset.rsxml";
               stream = assembly.GetManifestResourceStream(filename);
               ruleReader = new StreamReader(stream);*/

            uri = new Uri("/Rules/RFlagCleanRuleset.rsxml", UriKind.Relative);
            info = Application.GetResourceStream(uri);
            ruleReader = new StreamReader(info.Stream);

            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            RFlagCleanRuleset = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
           // RFlagCleanRuleset.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = RFlagCleanRuleset.ruleFileNames.Count;
            RFlagCleanRuleset.rules = LoadRulesFromFileNames(RFlagCleanRuleset.rulesDir, RFlagCleanRuleset.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in RFlagCleanRuleset.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            #endregion

            #region RuleSet - 8 - ICFixTotalRuleset
            /*filename = "Rules\\ICFixTotalRuleset.rsxml";
            //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
            ruleReader = new StreamReader(filename); */

            /*   assembly = Assembly.GetExecutingAssembly();
               filename = "AVL_Prototype_1.Rules.ICFixTotalRuleset.rsxml";
               stream = assembly.GetManifestResourceStream(filename);
               ruleReader = new StreamReader(stream); */

            uri = new Uri("/Rules/ICFixTotalRuleset.rsxml", UriKind.Relative);
            info = Application.GetResourceStream(uri);
            ruleReader = new StreamReader(info.Stream);


            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            ICFixTotalRuleset = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
          //  ICFixTotalRuleset.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = ICFixTotalRuleset.ruleFileNames.Count;
            ICFixTotalRuleset.rules = LoadRulesFromFileNames(ICFixTotalRuleset.rulesDir, ICFixTotalRuleset.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in ICFixTotalRuleset.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            #endregion

            #region RuleSet - 9 - TransformerFlipRuleset
            /*filename = "Rules\\TransformerFlipRuleset.rsxml";
            //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
            ruleReader = new StreamReader(filename);*/


            /*   assembly = Assembly.GetExecutingAssembly();
               filename = "AVL_Prototype_1.Rules.TransformerFlipRuleset.rsxml";
               stream = assembly.GetManifestResourceStream(filename);
               ruleReader = new StreamReader(stream); */

            uri = new Uri("/Rules/TransformerFlipRuleset.rsxml", UriKind.Relative);
            info = Application.GetResourceStream(uri);
            ruleReader = new StreamReader(info.Stream);

            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            TransformerFlipRuleset = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
            //TransformerFlipRuleset.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = TransformerFlipRuleset.ruleFileNames.Count;
            TransformerFlipRuleset.rules = LoadRulesFromFileNames(TransformerFlipRuleset.rulesDir, TransformerFlipRuleset.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in TransformerFlipRuleset.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            #endregion

            #region RuleSet - 10 - TransformerFlipRuleset2
            /*   filename = "Rules\\TransformerFlipRuleset2.rsxml";
               //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
               ruleReader = new StreamReader(filename); */


            /*    assembly = Assembly.GetExecutingAssembly();
                filename = "AVL_Prototype_1.Rules.TransformerFlipRuleset2.rsxml";
                stream = assembly.GetManifestResourceStream(filename);
                ruleReader = new StreamReader(stream); */

            uri = new Uri("/Rules/TransformerFlipRuleset2.rsxml", UriKind.Relative);
            info = Application.GetResourceStream(uri);
            ruleReader = new StreamReader(info.Stream);


            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            TransformerFlipRuleset2 = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
          //  TransformerFlipRuleset2.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = TransformerFlipRuleset2.ruleFileNames.Count;
            TransformerFlipRuleset2.rules = LoadRulesFromFileNames(TransformerFlipRuleset2.rulesDir, TransformerFlipRuleset2.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in TransformerFlipRuleset2.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            #endregion

            #region RuleSet - 11 - Clean23RuleSet
            /*filename = "Rules\\Clean23Ruleset.rsxml";
            //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
            ruleReader = new StreamReader(filename); */


            /*    assembly = Assembly.GetExecutingAssembly();
                filename = "AVL_Prototype_1.Rules.Clean23Ruleset.rsxml";
                stream = assembly.GetManifestResourceStream(filename);
                ruleReader = new StreamReader(stream); */

            uri = new Uri("/Rules/Clean23Ruleset.rsxml", UriKind.Relative);
            info = Application.GetResourceStream(uri);
            ruleReader = new StreamReader(info.Stream);


            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            Clean23Ruleset = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
          //  Clean23Ruleset.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = Clean23Ruleset.ruleFileNames.Count;
            Clean23Ruleset.rules = LoadRulesFromFileNames(Clean23Ruleset.rulesDir, Clean23Ruleset.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in Clean23Ruleset.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            #endregion

            #region Ruleset- 12 - State Equations
/*
            //State_Ruleset_1

            filename = "Rules\\State_Ruleset_1.rsxml";
            //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
            ruleReader = new StreamReader(filename);
            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            State_Ruleset_1 = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
            State_Ruleset_1.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = State_Ruleset_1.ruleFileNames.Count;
            State_Ruleset_1.rules = LoadRulesFromFileNames(State_Ruleset_1.rulesDir, State_Ruleset_1.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in State_Ruleset_1.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }


            filename = "Rules\\State_Ruleset_2.rsxml";
            //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
            ruleReader = new StreamReader(filename);
            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            State_Ruleset_2 = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
            State_Ruleset_2.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = State_Ruleset_2.ruleFileNames.Count;
            State_Ruleset_2.rules = LoadRulesFromFileNames(State_Ruleset_2.rulesDir, State_Ruleset_2.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in State_Ruleset_2.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }
            //State_FormatGraph
            filename = "Rules\\State_FormatGraph.rsxml";
            //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
            ruleReader = new StreamReader(filename);
            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            State_FormatGraph = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
            State_FormatGraph.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = State_FormatGraph.ruleFileNames.Count;
            State_FormatGraph.rules = LoadRulesFromFileNames(State_FormatGraph.rulesDir, State_FormatGraph.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in State_FormatGraph.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }


            #region State - Junction Summation Rules
            //State_Ruleset_3
            filename = "Rules\\State_Ruleset_3.rsxml";
            ruleReader = new StreamReader(filename);
            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            State_Ruleset_3 = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
            State_Ruleset_3.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = State_Ruleset_3.ruleFileNames.Count;
            State_Ruleset_3.rules = LoadRulesFromFileNames(State_Ruleset_3.rulesDir, State_Ruleset_3.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in State_Ruleset_3.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            //State_FormatForSum
            filename = "Rules\\State_FormatForSum.rsxml";
            ruleReader = new StreamReader(filename);
            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            State_FormatForSum = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
            State_FormatForSum.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = State_FormatForSum.ruleFileNames.Count;
            State_FormatForSum.rules = LoadRulesFromFileNames(State_FormatForSum.rulesDir, State_FormatForSum.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in State_FormatForSum.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }
            //State_Sum_AddLabels
            filename = "Rules\\State_Sum_AddLabels.rsxml";
            ruleReader = new StreamReader(filename);
            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            State_Sum_AddLabels = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
            State_Sum_AddLabels.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = State_Sum_AddLabels.ruleFileNames.Count;
            State_Sum_AddLabels.rules = LoadRulesFromFileNames(State_Sum_AddLabels.rulesDir, State_Sum_AddLabels.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in State_Sum_AddLabels.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }



            //State_Sum_Remove
            filename = "Rules\\State_Sum_Remove.rsxml";
            ruleReader = new StreamReader(filename);
            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            State_Sum_Remove = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
            State_Sum_Remove.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = State_Sum_Remove.ruleFileNames.Count;
            State_Sum_Remove.rules = LoadRulesFromFileNames(State_Sum_Remove.rulesDir, State_Sum_Remove.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in State_Sum_Remove.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }


            #endregion










            #endregion

            #region Ruleset - 13 - Solidworks Simplification

            filename = "Rules\\SolidworksRuleset.rsxml";
            ruleReader = new StreamReader(filename);
            ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            SolidworksRuleset = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
            SolidworksRuleset.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";

            numRules = SolidworksRuleset.ruleFileNames.Count;
            SolidworksRuleset.rules = LoadRulesFromFileNames(SolidworksRuleset.rulesDir, SolidworksRuleset.ruleFileNames, out numLoaded);
            ruleReader.Dispose();
            foreach (var item in SolidworksRuleset.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }
            */
            #endregion
            
            





            ruleReader.Dispose();

            // System.IO.File.Delete(filepath);

            //  System.IO.Directory.Delete(extractPath, true); 
        }
         
        #endregion

        // Returns the graph of the current open tab
        public Graph getActiveGraph()
        {
            TabItem selectedItem = (TabItem)graphTabs.Items[graphTabs.SelectedIndex];
            return (Graph)selectedItem.Content;
        }

        public bool isGraphInteractive(Graph graph)
        {
            return graph == Graph_System;
        }

        public void generateBondGraph()
        {
            //first check if all velocities are in the same direction or opposite direction but not in perpendicular 
            //direction for masses

            //rulesets have been loaded
            //now the next step is to generate system graph in graphsynth format
            //before that we have to update the modifiers to match the grammar rules
            foreach (var n in Graph_System.elements)
            {
                foreach (var n1 in n.modifiers)
                {
                    if (n1.Value > 0)
                    {
                        if (n1.Key.ToString().Contains("PARALLEL"))
                            if (!n.labels.Contains("PAR"))
                                n.labels.Add("PAR");

                        if (n1.Key.ToString().Contains("FRICTION"))
                            if (!n.labels.Contains("Include_Friction"))
                                n.labels.Add("Include_Friction");
                        if (n1.Key.ToString().Contains("INERTIA"))
                            if (!n.labels.Contains("Include_Inertia"))
                                n.labels.Add("Include_Inertia");
                        if (n1.Key.ToString().Contains("STIFFNESS"))
                            if (!n.labels.Contains("Include_Stiffness"))
                                n.labels.Add("Include_Stiffness");
                        if (n1.Key.ToString().Contains("MASS"))
                            if (!n.labels.Contains("Include_Mass"))
                                n.labels.Add("Include_Mass");
                        if (n.elementName.Contains("System_MR_Belt") && n1.Key.ToString().Contains("DAMPING"))
                            if (!n.labels.Contains("Include_Friction"))
                                n.labels.Add("Include_Friction");
                        if (n1.Key == ModifierType.VELOCITY)
                        {
                            if (n1.Value > 0)
                            {
                                if (!n.labels.Contains("veladded"))
                                {
                                    n.labels.Add("veladded");

                                    string velvalue = "vel" + n1.Value.ToString();
                                    if (!n.labels.Contains(velvalue))
                                        n.labels.Add(velvalue);
                                }
                            }
                        }
                    }
                }
            }

            //each graph element in the UI need a name - to conform to GraphSynth node name requirements
            int i = 0;
            foreach (var n in Graph_System.elements)
                n.nodeName = n.componentName + (i++);



            rectangles_BG.Clear();
            rectangles_BG_Causality.Clear();
            rectangles_BG_Simplified.Clear();
            lines_BG.Clear();
            lines_BG_Causality.Clear();
            lines_BG_Simplified.Clear();
            connections_BG.Clear();
            connections_BG_Causality.Clear();
            connections_BG_Simplified.Clear();
            Graph_BG1.clear();
            Graph_BG2.clear();
            Graph_BG3.clear();

            //check if viewmodels are being cleared before new generation
            //now generating GraphSynth graph
            generateSystemGraph();

            //now remove all the labels that we added

            foreach (var n in Graph_System.elements)
            {
                foreach (var n1 in n.modifiers)
                {
                    if (n1.Value > 0)
                    {

                        if (n.labels.Contains("PAR"))
                            n.labels.Remove("PAR");

                        if (n.labels.Contains("Include_Friction"))
                            n.labels.Remove("Include_Friction");

                        if (n.labels.Contains("Include_Inertia"))
                            n.labels.Remove("Include_Inertia");

                        if (n.labels.Contains("Include_Stiffness"))
                            n.labels.Remove("Include_Stiffness");

                        if (n.labels.Remove("Include_Mass"))
                            n.labels.Remove("Include_Mass");

                        if (n1.Key == ModifierType.VELOCITY)
                        {
                            if (n1.Value > 0)
                            {
                                if (n.labels.Contains("veladded"))
                                {
                                    n.labels.Remove("veladded");

                                    //string velvalue = "vel" + n1.Value.ToString();
                                    if (n.labels.Contains("vel1"))
                                        n.labels.Remove("vel1");
                                    if (n.labels.Contains("vel2"))
                                        n.labels.Remove("vel2");
                                    if (n.labels.Contains("vel3"))
                                        n.labels.Remove("vel3");
                                    if (n.labels.Contains("vel4"))
                                        n.labels.Remove("vel4");
                                    if (n.labels.Contains("vel5"))
                                        n.labels.Remove("vel5");
                                    if (n.labels.Contains("vel6"))
                                        n.labels.Remove("vel6");
                                    if (n.labels.Contains("vel7"))
                                        n.labels.Remove("vel7");
                                    if (n.labels.Contains("vel8"))
                                        n.labels.Remove("vel8");

                                }
                            }
                        }
                    }
                }
            }

            //need to return bool value if vel directions are fine or not. 

            //assigning I: and C: nodes with some identifier



            // bool noGood = true;
            checkIfVelocityDirectionsAreOkay(out bool noGood);

            if (!noGood)
            {
                MessageBox.Show("Velocity directions are an issue. If this continues, please delete velocity directions and try again. Thanks!");
            }
            else
            {
                bondgraphBeforeSimplification();
                bondgraphSimplified();

                int ii = 0;
                foreach (var n in systemGraph.nodes)
                {

                    if (n.localLabels.Contains("I:"))

                        n.localLabels.Add("iadded" + (ii.ToString()));
                    if (n.localLabels.Contains("C:")
                        )
                        n.localLabels.Add("cadded" + (ii.ToString()));
                    ii++;
                }
                obtainCausality();
            }

            graphTabs.SelectedIndex = 3;
        }

        private void obtainCausality()
        {
            // TODO

            rectangles_BG_Causality.Clear();
            lines_BG_Causality.Clear();
            connections_BG_Causality.Clear();
            Graph_BG3.CausalOptions.IsEnabled = true;
            nodeLabelSorted.Clear();
            sortedIndices.Clear();
            Graph_BG3.CausalOptions.Items.Clear();

            finalresult.Clear();
            optiGraphs.Clear();

            //will just do one option for now, will figure out 

            #region initial causality
            Stack<designGraph> sysGraphs = new Stack<designGraph>();

            sysGraphs.Push(systemGraph.copy());
            var sys = sysGraphs.Pop();
            options = (NewCausalityMethodRuleset.recognize(sys, false, null));
            while (options.Count > 0)
            {
                options[0].apply(sys, null);
                options = NewCausalityMethodRuleset.recognize(sys, false, null);
            }

            sysGraphs.Push(sys);
            while (sysGraphs.Count > 0)
            {
                sys = sysGraphs.Pop();
                options = (NewCausalityMethodRuleset_2.recognize(sys, false, null));
                if (options.Count > 0)
                {
                    foreach (var opt in options)
                    {
                        var gra = sys.copy();
                        GraphSynth.Search.SearchProcess.transferLmappingToChild(gra, sys, opt.nodes, opt.arcs, opt.hyperarcs);
                        opt.apply(gra, null);
                        sysGraphs.Push(gra);
                    }
                }
                else
                    sys_Graphs.Add(sys);
            }

            foreach (var item in sys_Graphs)
            {
                sys = item.copy();
                options = (NewCausalityMethodRuleset_3.recognize(sys, false, null));

                while (options.Count > 0)
                {
                    options[0].apply(sys, null);
                    options = NewCausalityMethodRuleset_3.recognize(sys, false, null);
                }
                sysGraphs.Push(sys);

            }

            sys_Graphs.Clear();

            #endregion 


            while (sysGraphs.Count > 0)
            {
                sys = sysGraphs.Pop();
                options = (INVDMarkerRules.recognize(sys, false, null));
                while (options.Count > 0)
                {
                    options[0].apply(sys, null);
                    options = INVDMarkerRules.recognize(sys, false, null);
                }

                Stack<designGraph> graphss = new Stack<designGraph>();
                List<designGraph> graph_SSS = new List<designGraph>();
                graphss.Push(sys);

                {
                    var graphS = graphss.Pop();
                    var options1 = (CalibrationNewRuleset.recognize(graphS, false, null));

                    if (options1.Count > 0)
                    {
                        foreach (var opt in options1)
                        {
                            var graphSS = graphS.copy();
                            GraphSynth.Search.SearchProcess.transferLmappingToChild(graphSS, graphS, opt.nodes,
                                                               opt.arcs, opt.hyperarcs);
                            opt.apply(graphSS, null);
                            graph_SSS.Add(graphSS);
                        }
                    }
                    else
                        graph_SSS.Add(graphS);

                }

                foreach (var opt in graph_SSS)
                {
                    var options1 = (CalibrationNewRuleset.recognize(opt, false, null));

                    while (options1.Count > 0)
                    {
                        options1[0].apply(opt, null);
                        options1 = CalibrationNewRuleset.recognize(opt, false, null);
                    }

                    graphss.Push(opt);
                }

                graph_SSS.Clear();

                while (graphss.Count > 0)
                {
                    var graphS = graphss.Pop();
                    var options1 = (RFlagCleanRuleset.recognize(graphS, false, null));

                    /* if (options1.Count > 0)
                     {
                         foreach (var opt in options1)
                         {
                             var graphSS = graphS.copy();
                             GraphSynth.Search.SearchProcess.transferLmappingToChild(graphSS, graphS, opt.nodes,
                                                                opt.arcs, opt.hyperarcs);
                             opt.apply(graphSS, null);
                             graphss.Push(graphSS);
                         }
                     }
                     else
                         graph_SSS.Add(graphS); */
                    while (options1.Count > 0)
                    {
                        options1[0].apply(graphS, null);
                        options1 = (RFlagCleanRuleset.recognize(graphS, false, null));
                    }
                    graph_SSS.Add(graphS);
                }
                foreach (var opt in graph_SSS)
                {
                    graphss.Push(opt);
                }

                graph_SSS.Clear();

                while (graphss.Count > 0)
                {
                    var graphS = graphss.Pop();
                    var options1 = (ICFixTotalRuleset.recognize(graphS, false, null));

                    /* if (options1.Count > 0)
                     {
                         foreach (var opt in options1)
                         {
                             var graphSS = graphS.copy();
                             GraphSynth.Search.SearchProcess.transferLmappingToChild(graphSS, graphS, opt.nodes,
                                                                opt.arcs, opt.hyperarcs);
                             opt.apply(graphSS, null);
                             graphss.Push(graphSS);
                         }
                     }
                     else
                         graph_SSS.Add(graphS);*/

                    while (options1.Count > 0)
                    {
                        options1[0].apply(graphS, null);
                        options1 = (ICFixTotalRuleset.recognize(graphS, false, null));
                    }

                    graph_SSS.Add(graphS);

                }

                foreach (var op in graph_SSS)
                {
                    options = (TransformerFlipRuleset.recognize(op, false, null));

                    if (options.Count > 0)
                    {
                        options[0].apply(op, null);

                        sysGraphs.Push(op);

                    }

                    else
                    {


                        options = (TransformerFlipRuleset2.recognize(op, false, null));

                        if (options.Count > 0)
                        {

                            options[0].apply(op, null);
                            sysGraphs.Push(op);
                        }
                        else
                            optiGraphs.Add(op);
                    }


                }

            }

            foreach (var opt in optiGraphs)
            {
                options = (Clean23Ruleset.recognize(opt, false, null));

                while (options.Count > 0)
                {
                    options[0].apply(opt, null);
                    options = Clean23Ruleset.recognize(opt, false, null);
                }

                finalresult.Add(opt);
            }






            indiceswithoutINVD.Clear();
            maxIntegralCausality.Clear();
            for (int ii = 0; ii < finalresult.Count; ii++)
            {
                bool index = checkINVD(finalresult[ii]);

                if (index == false)
                {
                    indiceswithoutINVD.Add(ii);
                    maxIntegralCausality.Add(checkICs(finalresult[ii]));

                }
            }

            //now from the list of finalgraph, eliminate duplicate solutions



            if (indiceswithoutINVD.Count == 0)
            {
                MessageBox.Show("Sorry, we have encountered an error with respect to Causality assignment");

            }

            //need to add exception here if the program is unable to added 

            else
            {
                int currentHashSetcount = 0;
                int nn = 0;
                foreach (var n in indiceswithoutINVD)
                {
                    List<string> nodeLabels_Cau = new List<string>();
                    var cauGraph = finalresult[n];

                    foreach (var arcC in cauGraph.arcs)
                    {
                        if (arcC.localLabels.Contains("I2") && arcC.localLabels.Contains("SAME"))
                        {
                            for (int iii = 0; iii < arcC.To.localLabels.Count; iii++)
                            {
                                if (arcC.To.localLabels[iii].Contains("iadded"))
                                {
                                    nodeLabels_Cau.Add(arcC.To.localLabels[iii]);

                                }

                            }
                        }
                        if (arcC.localLabels.Contains("C3") && arcC.localLabels.Contains("OPP"))
                        {
                            for (int iii = 0; iii < arcC.From.localLabels.Count; iii++)
                            {
                                if (arcC.From.localLabels[iii].Contains("cadded"))
                                {
                                    nodeLabels_Cau.Add(arcC.From.localLabels[iii]);

                                }

                            }
                        }

                    }

                    if (nodeLabels_Cau.Count > 0)
                    {
                        nodeLabels_Cau.Sort();

                        string combined = "";
                        foreach (var x in nodeLabels_Cau)
                            combined += x;

                        nodeLabelSorted.Add(combined);

                        //need to add index numbers as well into a list

                        if ((nn + 1) == nodeLabelSorted.Count) //for the first one, it will be same
                        {
                            sortedIndices.Add(n);
                            currentHashSetcount++;
                        }

                        if (currentHashSetcount <= nodeLabelSorted.Count)
                        {
                            sortedIndices.Add(n);
                            currentHashSetcount++;
                        }

                    }
                    nn++;
                }
                // nodeNames_Cau.Sort();
                // nodeNames.Add(nodeNames_Cau);

                List<int> maxIntegralCaus = new List<int>();

                foreach (var n in sortedIndices)
                {
                    //use the indiceswithoutINVD to obtain the index in that list

                    var indexindex = indiceswithoutINVD.FindIndex(item => item == n);
                    maxIntegralCaus.Add(maxIntegralCausality[indexindex]);

                }

                index1 = maxIntegralCausality.IndexOf(maxIntegralCausality.Max());

                //  foreach (var no in finalresult[index1].nodes)

                //now add to the combo-box

                for (int pp = 0; pp < nodeLabelSorted.Count; pp++)
                {
                    var stringtobeadded = "Option " + (pp + 1).ToString();

                    Graph_BG3.CausalOptions.Items.Add(stringtobeadded);
                }

                Graph_BG3.CausalOptions.IsEnabled = true;


                rectangles_BG_Causality.Clear();
                lines_BG_Causality.Clear();
                connections_BG_Causality.Clear();
                causality = true;

                /*
                foreach (var no in finalresult[indiceswithoutINVD[index1]].nodes)
                {
                    var rect = new RectangleViewModel();

                    rect.NodeName = no.name;
                    //rect.Content = "";
                    List<string> localLabels = no.localLabels.ToList();

                    List<string> localLabels_Copy = no.localLabels.ToList();

                    foreach (var uu in localLabels_Copy)
                    {
                        if (uu.Contains("vel"))
                            localLabels.Remove(uu);
                        if (uu.Contains("good"))
                            localLabels.Remove(uu);
                        if (uu.Contains("multiple"))
                            localLabels.Remove(uu);
                        if (uu.Contains("system"))
                            localLabels.Remove(uu);
                        if (uu.Contains("iadded"))
                            localLabels.Remove(uu);
                        if (uu.Contains("cadded"))
                            localLabels.Remove(uu);
                        if (uu.Contains("rackadded"))
                            localLabels.Remove(uu);
                        if (uu.Contains("gearadded"))
                            localLabels.Remove(uu);
                        if (uu.Contains("layoutadded"))
                            localLabels.Remove(uu);

                    }

                    rect.Content = string.Join(" ", localLabels.ToArray());
                    rect.X = no.X;
                    rect.Y = no.Y;
                    rect.Color = "Black";
                    rect.Font = "Segoe UI";
                    rect.Width = 75;
                    rect.Height = 20;
                    rectangles_BG_Causality.Add(rect);

                }

                int p = 101;
                //foreach (var no in finalresult[index1].arcs)
                foreach (var no in finalresult[indiceswithoutINVD[index1]].arcs)
                {

                    var line = new LineConnections();

                    line.Name = no.name;
                    line.LC = "Red";
                    line.LTA = "arrow";
                    line.LTD = string.Join(" ", no.localLabels.ToArray());


                    line.Thickness = 1;

                    if (no.localLabels.Contains("SAME"))
                    {
                        line.X1 = no.From.X;
                        line.Y1 = no.From.Y;
                        line.X2 = no.To.X;
                        line.Y2 = no.To.Y;
                        line.ArrowEnd = "same";

                        if (no.localLabels.Contains("I2"))
                            line.LC = "Blue";


                    }
                    else
                    {
                        line.X1 = no.To.X;
                        line.Y1 = no.To.Y;
                        line.X2 = no.From.X;
                        line.Y2 = no.From.Y;
                        line.ArrowEnd = "opp";

                        if (no.localLabels.Contains("C3"))
                            line.LC = "Blue";
                    }

                    var connect = new ConnectionViewModel();



                    lines_BG_Causality.Add(line);
                    connect.Line = line;

                    foreach (var rects in rectangles_BG_Causality)
                    {
                        if (rects.NodeName == no.From.name)
                            connect.Rect1 = rects;
                        if (rects.NodeName == no.To.name)
                            connect.Rect2 = rects;

                    }

                    connect.ConnectionMultiple = 1;
                    connect.ConnectionSide = "causal_assigned";
                    connections_BG_Causality.Add(connect);

                }

                HashSet<string> rectNames = new HashSet<string>();
                bool trial = false;
                bool trial1 = false;

                TextBlock ex = null;

                Line redLine = null;

                int i = 0;

                List<string> stringLine = new List<string>();
                List<int> indices = new List<int>();

                for (int j = 0; j < rectangles_BG_Causality.Count; j++)
                {
                    int count = 0;
                    foreach (var item in connections_BG_Causality)
                        if (rectangles_BG_Causality[j].NodeName == item.Rect1.NodeName || rectangles_BG_Causality[j].NodeName == item.Rect2.NodeName)
                            count++;

                    if (count == 0)
                        indices.Add(j);

                }

                if (indices.Count > 0)
                {
                    for (int j = 0; j < indices.Count; j++)
                    {
                        ex = new TextBlock();
                        ex.Text = rectangles_BG_Causality[indices[j]].Content;
                        ex.SetValue(Canvas.TopProperty, rectangles_BG_Causality[indices[j]].Y);
                        ex.SetValue(Canvas.LeftProperty, rectangles_BG_Causality[indices[j]].X);

                        ex.Visibility = System.Windows.Visibility.Visible;
                        ex.Name = rectangles_BG_Causality[indices[j]].NodeName;
                        ex.TextWrapping = TextWrapping.Wrap;
                        ex.FontFamily = new FontFamily(rectangles_BG_Causality[indices[j]].Font);
                        ex.Foreground = findColor(rectangles_BG_Causality[indices[j]].Color);
                        ex.FontSize = FontSize;

                        ex.MouseLeftButtonDown += new MouseButtonEventHandler(ex_B_G_C_MouseDown);
                        ex.MouseMove += new MouseEventHandler(ex_B_G_C_MouseMove);
                        ex.MouseLeftButtonUp += new MouseButtonEventHandler(ex_B_G_C_MouseUp);
                        ex.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                        ex.Arrange(new Rect(ex.DesiredSize));

                        foreach (var tmp in rectangles_BG_Causality)
                            if (tmp.NodeName == ex.Name)
                            {
                                tmp.Width = ex.ActualWidth;
                                tmp.Height = ex.ActualHeight;

                            }
                        Graph_BG3.theCanvas.Children.Add(ex);
                    }
                }
                */
            }

            Graph_BG3.CausalOptions.SelectedIndex = 0;
            causaloptions_selection();
        }

        internal void causaloptions_selection()
        {
            int s = Graph_BG3.CausalOptions.SelectedIndex;

            if (s == -1)
                return;

            rectangles_BG_Causality.Clear();
            lines_BG_Causality.Clear();
            connections_BG_Causality.Clear();
            causality = true;

            Graph_BG3.clear();

            Graph_BG1.bigCanvas.AllowDrop = false;
            Graph_BG2.bigCanvas.AllowDrop = false;
            Graph_BG3.bigCanvas.AllowDrop = false;

            List<int> sortIndex = sortedIndices.ToList();

            int indexX = sortIndex[s];


            foreach (var no in finalresult[indexX].nodes)
            {
                var rect = new RectangleViewModel();

                rect.NodeName = no.name;
                List<string> localLabels = no.localLabels.ToList();

                List<string> localLabels_Copy = no.localLabels.ToList();

                foreach (var uu in localLabels_Copy)
                {
                    if (uu.Contains("vel"))
                        localLabels.Remove(uu);
                    if (uu.Contains("good"))
                        localLabels.Remove(uu);
                    if (uu.Contains("multiple"))
                        localLabels.Remove(uu);
                    if (uu.Contains("system"))
                        localLabels.Remove(uu);
                    if (uu.Contains("iadded"))
                        localLabels.Remove(uu);
                    if (uu.Contains("cadded"))
                        localLabels.Remove(uu);
                    if (uu.Contains("rackadded"))
                        localLabels.Remove(uu);
                    if (uu.Contains("gearadded"))
                        localLabels.Remove(uu);
                    if (uu.Contains("layoutadded"))
                        localLabels.Remove(uu);

                }

                rect.Content = string.Join(" ", localLabels.ToArray());
                rect.X = no.X;
                rect.Y = no.Y;
                rect.Color = "Black";
                rect.Font = "Segoe UI";
                rect.Width = 75;
                rect.Height = 20;
                rectangles_BG_Causality.Add(rect);

            }

            int p = 101;
            //foreach (var no in finalresult[index1].arcs)
            foreach (var no in finalresult[indexX].arcs)
            {

                var line = new LineConnections();

                line.Name = no.name;
                line.LC = "Red";
                line.LTA = "arrow";
                line.LTD = string.Join(" ", no.localLabels.ToArray());


                line.Thickness = 1;

                if (no.localLabels.Contains("SAME"))
                {
                    line.X1 = no.From.X;
                    line.Y1 = no.From.Y;
                    line.X2 = no.To.X;
                    line.Y2 = no.To.Y;
                    line.ArrowEnd = "same";

                    if (no.localLabels.Contains("I2"))
                        line.LC = "Blue";


                }
                else
                {
                    line.X1 = no.To.X;
                    line.Y1 = no.To.Y;
                    line.X2 = no.From.X;
                    line.Y2 = no.From.Y;
                    line.ArrowEnd = "opp";

                    if (no.localLabels.Contains("C3"))
                        line.LC = "Blue";
                }

                var connect = new ConnectionViewModel();



                lines_BG_Causality.Add(line);
                connect.Line = line;

                foreach (var rects in rectangles_BG_Causality)
                {
                    if (rects.NodeName == no.From.name)
                        connect.Rect1 = rects;
                    if (rects.NodeName == no.To.name)
                        connect.Rect2 = rects;

                }

                connect.ConnectionMultiple = 1;
                connect.ConnectionSide = "causal_assigned";
                connections_BG_Causality.Add(connect);

            }

            Dictionary<RectangleViewModel, BondGraphElement> rectElements = new Dictionary<RectangleViewModel, BondGraphElement>();

            // Create all BondGraphElements
            foreach (RectangleViewModel r in rectangles_BG_Causality)
            {
                BondGraphElement bgElement = new BondGraphElement(Graph_BG3, r.NodeName, r.Content, new Point(r.X, r.Y));
                rectElements.Add(r, bgElement);
            }

            // Create all BondArcs
            foreach (ConnectionViewModel c in connections_BG_Causality)
            {
                BondGraphElement el1 = rectElements[c.Rect1];
                BondGraphElement el2 = rectElements[c.Rect2];

                int arrowDir = c.Line.ArrowEnd.Contains("opp") ? 1 : 2;
                int causalDir = 2;

                BondGraphArc bgArc = new BondGraphArc(el1, el2, new SolidColorBrush(c.Line.LC == "Blue" ? Colors.Blue : Colors.Red), arrowDir, causalDir);
            }

            /*
            List<string> stringLine = new List<string>();
            List<int> indices = new List<int>();

            TextBlock ex = null;
            HashSet<string> rectNames = new HashSet<string>();

            for (int j = 0; j < rectangles_BG_Causality.Count; j++)
            {
                int count = 0;
                foreach (var item in connections_BG_Causality)
                    if (rectangles_BG_Causality[j].NodeName == item.Rect1.NodeName || rectangles_BG_Causality[j].NodeName == item.Rect2.NodeName)
                        count++;

                if (count == 0)
                    indices.Add(j);
            }

            if (indices.Count > 0)
            {
                for (int j = 0; j < indices.Count; j++)
                {

                    ex = new TextBlock();
                    ex.Text = rectangles_BG_Causality[indices[j]].Content;
                    ex.SetValue(Canvas.TopProperty, rectangles_BG_Causality[indices[j]].Y);
                    ex.SetValue(Canvas.LeftProperty, rectangles_BG_Causality[indices[j]].X);

                    ex.Visibility = System.Windows.Visibility.Visible;
                    ex.Name = rectangles_BG_Causality[indices[j]].NodeName;
                    ex.TextWrapping = TextWrapping.Wrap;
                    ex.FontFamily = new FontFamily(rectangles_BG_Causality[indices[j]].Font);
                    ex.Foreground = findColor(rectangles_BG_Causality[indices[j]].Color);
                    ex.FontSize = FontSize;

                    ex.MouseLeftButtonDown += new MouseButtonEventHandler(ex_B_G_C_MouseDown);
                    ex.MouseMove += new MouseEventHandler(ex_B_G_C_MouseMove);
                    ex.MouseLeftButtonUp += new MouseButtonEventHandler(ex_B_G_C_MouseUp);
                    ex.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                    ex.Arrange(new Rect(ex.DesiredSize));

                    foreach (var tmp in rectangles_BG_Causality)
                        if (tmp.NodeName == ex.Name)
                        {
                            tmp.Width = ex.ActualWidth;
                            tmp.Height = ex.ActualHeight;

                        }
                    Graph_BG3.theCanvas.Children.Add(ex);
                }
            }
            */

            Graph_BG1.bigCanvas.AllowDrop = false;
            Graph_BG2.bigCanvas.AllowDrop = false;
            Graph_BG3.bigCanvas.AllowDrop = false;
        }

        private int checkICs(designGraph designGraph)
        {
            int xx = 0;
            foreach (arc a in designGraph.arcs)
            {
                if (a.localLabels.Contains("I2") && a.localLabels.Contains("SAME"))
                    xx = xx + 1;
                if (a.localLabels.Contains("C3") && a.localLabels.Contains("OPP"))
                    xx = xx + 1;
            }

            return (xx);

        }

        private bool checkINVD(designGraph designGraph)
        {
            foreach (node n in designGraph.nodes)
            {
                foreach (string x in n.localLabels)
                {
                    if (x.Contains("INVD"))
                        return true;
                    if (x.Contains("Flipped"))
                        return true;
                }

            }
            return false;
        }
        private void checkIfVelocityDirectionsAreOkay(out bool noGood)
        {
            //works for mechanical translation systems only - no mechanical rotation yet

            //first load the verify direction rules 

            /*var filename = "Rules\\BeforeBG-VerifyDirRuleSet.rsxml";
            //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
            ruleReader = new StreamReader(filename);*/


            /*   var assembly = Assembly.GetExecutingAssembly();
               var filename = "AVL_Prototype_1.Rules.BeforeBG-VerifyDirRuleSet.rsxml";
               var stream = assembly.GetManifestResourceStream(filename);
               ruleReader = new StreamReader(stream); */

            Uri uri = new Uri("/Rules/BeforeBG-VerifyDirRuleSet.rsxml", UriKind.Relative);
            System.Windows.Resources.StreamResourceInfo info = Application.GetResourceStream(uri);
            ruleReader = new StreamReader(info.Stream);

            var ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            ruleSet VerifyBGDir = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
            //VerifyBGDir.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";
            int numLoaded;
            var numRules = VerifyBGDir.ruleFileNames.Count;
            VerifyBGDir.rules = LoadRulesFromFileNames(VerifyBGDir.rulesDir, VerifyBGDir.ruleFileNames, out numLoaded);
            ruleReader.Dispose();

            foreach (var item in VerifyBGDir.rules)
            {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            //second step is to verify if all nodes have velocity directions 

            options = (VerifyBGDir.recognize(systemGraph, false, null));

            while (options.Count > 0)
            {
                options[0].apply(systemGraph, null);
                options = SimplificationRuleset.recognize(systemGraph, false, null);
            }

            //now check if all the nodes that have veladded label have good label as well

            noGood = true;

            foreach (var n in systemGraph.nodes)
            {
                if (n.localLabels.Contains("veladded") && !n.localLabels.Contains("good"))
                {
                    noGood = false;
                    break;
                }
            }
        }

        private void bondgraphSimplified()
        {
            options = (SimplificationRuleset.recognize(systemGraph, false, null));

            while (options.Count > 0)
            {
                options[0].apply(systemGraph, null);
                options = SimplificationRuleset.recognize(systemGraph, false, null);
            }

            options = (directionRuleSet.recognize(systemGraph, false, null));

            while (options.Count > 0)
            {
                options[0].apply(systemGraph, null);
                options = directionRuleSet.recognize(systemGraph, false, null);
            }

            options = (directionRuleSet2.recognize(systemGraph, false, null));

            while (options.Count > 0)
            {
                options[0].apply(systemGraph, null);
                options = directionRuleSet2.recognize(systemGraph, false, null);
            }

            options = (directionRuleSet3.recognize(systemGraph, false, null));

            while (options.Count > 0)
            {
                options[0].apply(systemGraph, null);
                options = directionRuleSet3.recognize(systemGraph, false, null);
            }

            options = (SimplificationRuleset2.recognize(systemGraph, false, null));

            while (options.Count > 0)
            {
                options[0].apply(systemGraph, null);
                options = SimplificationRuleset2.recognize(systemGraph, false, null);
            }

            //again do a deepcopy of the systemGraph 

            //  designGraph SimplifiedGraphWithDir = systemGraph.copy(true);

            rectangles_BG_Simplified.Clear();
            lines_BG_Simplified.Clear();
            connections_BG_Simplified.Clear();

            foreach (var no in systemGraph.nodes)
            {
                var rect = new RectangleViewModel();

                rect.NodeName = no.name;
                List<string> localLabels = no.localLabels.ToList();

                List<string> localLabels_Copy = no.localLabels.ToList();

                foreach (var uu in localLabels_Copy)
                {
                    if (uu.Contains("vel"))
                        localLabels.Remove(uu);
                    if (uu.Contains("good"))
                        localLabels.Remove(uu);
                    if (uu.Contains("multiple"))
                        localLabels.Remove(uu);
                    if (uu.Contains("system"))
                        localLabels.Remove(uu);
                    if (uu.Contains("iadded"))
                        localLabels.Remove(uu);
                    if (uu.Contains("cadded"))
                        localLabels.Remove(uu);
                    if (uu.Contains("rackadded"))
                        localLabels.Remove(uu);
                    if (uu.Contains("gearadded"))
                        localLabels.Remove(uu);
                    if (uu.Contains("layoutadded"))
                        localLabels.Remove(uu);

                }

                rect.Content = string.Join(" ", localLabels.ToArray());
                rect.X = no.X;
                rect.Y = no.Y;
                rect.Color = "Black";
                rect.Font = "Segoe UI";
                rect.Width = 75;
                rect.Height = 20;
                rectangles_BG_Simplified.Add(rect);
            }

            int p = 101;
            foreach (var no in systemGraph.arcs)
            {
                if (no.localLabels.Contains("dir"))
                {
                    var line = new LineConnections();

                    line.Name = no.name;
                    line.LC = "Red";
                    line.LTA = "arrow";
                    line.LTD = string.Join(" ", no.localLabels.ToArray());

                    line.Thickness = 1;
                    line.X1 = no.From.X;
                    line.Y1 = no.From.Y;
                    line.X2 = no.To.X;
                    line.Y2 = no.To.Y;

                    var connect = new ConnectionViewModel();

                    lines_BG_Simplified.Add(line);
                    connect.Line = line;

                    foreach (var rects in rectangles_BG_Simplified)
                    {
                        if (rects.NodeName == no.From.name)
                            connect.Rect1 = rects;
                        if (rects.NodeName == no.To.name)
                            connect.Rect2 = rects;

                    }

                    connect.ConnectionMultiple = 1;
                    connect.ConnectionSide = "dir";
                    connections_BG_Simplified.Add(connect);
                }
                else
                {
                    var line = new LineConnections();

                    line.Name = no.name;
                    line.LC = "Red";
                    line.LTA = "noarrow";
                    line.LTD = string.Join(" ", no.localLabels.ToArray());

                    line.Thickness = 1;
                    line.X1 = no.From.X;
                    line.Y1 = no.From.Y;
                    line.X2 = no.To.X;
                    line.Y2 = no.To.Y;

                    var connect = new ConnectionViewModel();

                    lines_BG_Simplified.Add(line);
                    connect.Line = line;

                    foreach (var rects in rectangles_BG_Simplified)
                    {
                        if (rects.NodeName == no.From.name)
                            connect.Rect1 = rects;
                        if (rects.NodeName == no.To.name)
                            connect.Rect2 = rects;

                    }

                    connect.ConnectionMultiple = 1;
                    connect.ConnectionSide = "nondir";
                    connections_BG_Simplified.Add(connect);
                }
            }

            //now let us apply directions - first let us do the sources and i-c-r 

            //all sources should have arrow heads away from source 
            //all I-C-R should have arrow heads into them. 

            Dictionary<RectangleViewModel, BondGraphElement> rectElements = new Dictionary<RectangleViewModel, BondGraphElement>();

            // Create all BondGraphElements
            foreach (RectangleViewModel r in rectangles_BG_Simplified)
            {
                BondGraphElement bgElement = new BondGraphElement(Graph_BG2, r.NodeName, r.Content, new Point(r.X, r.Y));
                rectElements.Add(r, bgElement);
            }

            // Create all BondArcs
            foreach (ConnectionViewModel c in connections_BG_Simplified)
            {
                if (!c.ConnectionSide.Contains("nondir"))
                {
                    BondGraphElement el1 = rectElements[c.Rect1];
                    BondGraphElement el2 = rectElements[c.Rect2];

                    int arrowDir = c.Line.ArrowEnd == "rect1" ? 1 : 2;
                    int causalDir = 0;

                    BondGraphArc bgArc = new BondGraphArc(el1, el2, new SolidColorBrush(Colors.Red), arrowDir, causalDir);
                }
            }

            /*
            List<string> stringLine = new List<string>();
            List<int> indices = new List<int>();

            HashSet<string> rectNames = new HashSet<string>();
            TextBlock ex = null;

            for (int j = 0; j < rectangles_BG_Simplified.Count; j++)
            {
                int count = 0;
                foreach (var item in connections_BG_Simplified)
                    if (rectangles_BG_Simplified[j].NodeName == item.Rect1.NodeName || rectangles_BG_Simplified[j].NodeName == item.Rect2.NodeName)
                        count++;

                if (count == 0)
                    indices.Add(j);
            }

            if (indices.Count > 0)
            {
                for (int j = 0; j < indices.Count; j++)
                {
                    ex = new TextBlock();
                    ex.Text = rectangles_BG_Simplified[indices[j]].Content;
                    ex.SetValue(Canvas.TopProperty, rectangles_BG_Simplified[indices[j]].Y);
                    ex.SetValue(Canvas.LeftProperty, rectangles_BG_Simplified[indices[j]].X);

                    ex.Visibility = System.Windows.Visibility.Visible;
                    ex.Name = rectangles_BG_Simplified[indices[j]].NodeName;
                    ex.TextWrapping = TextWrapping.Wrap;
                    ex.FontFamily = new FontFamily(rectangles_BG_Simplified[indices[j]].Font);
                    ex.Foreground = findColor(rectangles_BG_Simplified[indices[j]].Color);
                    ex.FontSize = FontSize;

                    ex.MouseLeftButtonDown += new MouseButtonEventHandler(ex_BG_MouseDown);
                    ex.MouseMove += new MouseEventHandler(ex_B_G_MouseMove);
                    ex.MouseLeftButtonUp += new MouseButtonEventHandler(ex_B_G_MouseUp);
                    ex.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                    ex.Arrange(new Rect(ex.DesiredSize));

                    foreach (var tmp in rectangles_BG_Simplified)
                    {
                        if (tmp.NodeName == ex.Name)
                        {
                            tmp.Width = ex.ActualWidth;
                            tmp.Height = ex.ActualHeight;
                        }
                    }
                    Graph_BG2.theCanvas.Children.Add(ex);
                }
            }
            */
        }

        private void LineVector(Point[] points, string p, out double Cnew_x, out double Cnew_y, out double Cnew_x_1, out double Cnew_y_1)
        {
            double x1, y1, x2, y2;
            x1 = points[0].X;
            y1 = points[0].Y;
            x2 = points[1].X;
            y2 = points[1].Y;

            /*  if (p.Contains("same"))
              {

              }
              else
              {
                  x1 = points[1].X;
                  y1 = points[1].Y;
                  x2 = points[0].X;
                  y2 = points[0].Y;

              } */

            var angle = Math.Atan2(y2 - y1, (x2 - x1)) * 180 / Math.PI;

            var distance = Math.Sqrt(Math.Pow(y2 - y1, 2) + Math.Pow(x2 - x1, 2));

            var newangle = (angle + 90);

            if (p.Contains("opp"))
            {
                Cnew_x = x1 + Math.Cos(newangle * Math.PI / 180) * distance * 0.1;
                Cnew_y = y1 + Math.Sin(newangle * Math.PI / 180) * distance * 0.1;

                var newangle1 = angle - 90;

                Cnew_x_1 = x1 + Math.Cos(newangle1 * Math.PI / 180) * distance * 0.1;
                Cnew_y_1 = y1 + Math.Sin(newangle1 * Math.PI / 180) * distance * 0.1;
            }
            else
            {
                Cnew_x = x2 + Math.Cos(newangle * Math.PI / 180) * distance * 0.1;
                Cnew_y = y2 + Math.Sin(newangle * Math.PI / 180) * distance * 0.1;

                var newangle1 = angle - 90;

                Cnew_x_1 = x2 + Math.Cos(newangle1 * Math.PI / 180) * distance * 0.1;
                Cnew_y_1 = y2 + Math.Sin(newangle1 * Math.PI / 180) * distance * 0.1;

            }
        }

        private void LineVector(Point[] points, out double Cnew_x, out double Cnew_y, out double Cnew_x_1, out double Cnew_y_1)
        {
            double x1, y1, x2, y2;
            x1 = points[0].X;
            y1 = points[0].Y;
            x2 = points[1].X;
            y2 = points[1].Y;

            /*  if (p.Contains("same"))
              {
               
              }
              else
              {
                  x1 = points[1].X;
                  y1 = points[1].Y;
                  x2 = points[0].X;
                  y2 = points[0].Y;

              } */

            var angle = Math.Atan2(y2 - y1, (x2 - x1)) * 180 / Math.PI;

            var distance = Math.Sqrt(Math.Pow(y2 - y1, 2) + Math.Pow(x2 - x1, 2));

            var newangle = (angle + 90);

            Cnew_x = x2 + Math.Cos(newangle * Math.PI / 180) * distance * 0.1;
            Cnew_y = y2 + Math.Sin(newangle * Math.PI / 180) * distance * 0.1;

            var newangle1 = angle - 90;

            Cnew_x_1 = x2 + Math.Cos(newangle1 * Math.PI / 180) * distance * 0.1;
            Cnew_y_1 = y2 + Math.Sin(newangle1 * Math.PI / 180) * distance * 0.1;
        }

        private void vectorComp(Point[] points, out double new_x, out double new_y, out double new_x_1, out double new_y_1, out double new_x_2, out double new_y_2)
        {
            var angle = Math.Atan2(points[1].Y - points[0].Y, (points[1].X - points[0].X)) * 180 / Math.PI;

            var distance = Math.Sqrt(Math.Pow(points[0].X - points[1].X, 2) + Math.Pow(points[0].Y - points[1].Y, 2));

            var x_intercept = (points[1].X - points[0].X) / distance;
            var y_intercept = (points[1].Y - points[0].Y) / distance;

            new_x = points[0].X + 0.20 * distance * x_intercept;
            new_y = points[0].Y + 0.20 * distance * y_intercept;

            new_x_1 = points[0].X + 0.80 * distance * x_intercept;
            new_y_1 = points[0].Y + 0.80 * distance * y_intercept;

            var newangle = (angle + 20);

            new_x_2 = new_x_1 - Math.Cos(newangle * Math.PI / 180) * distance * 0.25;
            new_y_2 = new_y_1 - Math.Sin(newangle * Math.PI / 180) * distance * 0.25;
        }

        private void ex_BG_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TextBlock item = sender as TextBlock;
            mousePoint = new Point(e.GetPosition(Graph_BG1.theCanvas).X, e.GetPosition(Graph_BG1.theCanvas).Y);
            item.ReleaseMouseCapture();
            isMouseCaptured = false;
        }

        private void ex_B_G_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TextBlock item = sender as TextBlock;
            mousePoint = new Point(e.GetPosition(Graph_BG2.theCanvas).X, e.GetPosition(Graph_BG2.theCanvas).Y);
            item.ReleaseMouseCapture();
            isMouseCaptured = false;
        }


        private void ex_B_G_C_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TextBlock item = sender as TextBlock;
            mousePoint = new Point(e.GetPosition(Graph_BG3.theCanvas).X, e.GetPosition(Graph_BG3.theCanvas).Y);
            item.ReleaseMouseCapture();
            isMouseCaptured = false;
        }

        private void ex_BG_MouseMove(object sender, MouseEventArgs e)
        {
            TextBlock item = sender as TextBlock;
            if (isMouseCaptured)
            {
                Point originalPoint = new Point((double)item.GetValue(Canvas.LeftProperty), (double)item.GetValue(Canvas.TopProperty));

                // Calculate the current position of the object.
                double deltaV = e.GetPosition(null).Y - mouseVerticalPosition;
                double deltaH = e.GetPosition(null).X - mouseHorizontalPosition;
                double newTop = deltaV + (double)item.GetValue(Canvas.TopProperty);
                double newLeft = deltaH + (double)item.GetValue(Canvas.LeftProperty);

                // Set new position of object.
                item.SetValue(Canvas.TopProperty, newTop);
                item.SetValue(Canvas.LeftProperty, newLeft);

                // Update position global variables.
                mouseVerticalPosition = e.GetPosition(null).Y;
                mouseHorizontalPosition = e.GetPosition(null).X;

                //should update the connection lines and so on

                foreach (var elements in rectangles_BG)
                {
                    if (elements.NodeName == item.Name)
                    {
                        elements.X = newLeft;
                        elements.Y = newTop;
                    }

                }

                int index = 0;
                foreach (var canvasLines in Graph_BG1.theCanvas.Children)
                {
                    if (canvasLines.GetType().Name == "Line")
                    {
                        var canLin = canvasLines as Line;
                        foreach (var chi in connections_BG)
                        {
                            if (chi.Line.Name == canLin.Name)
                            {
                                if (chi.Rect1.NodeName == item.Name)
                                {
                                    Point updatePoint = new Point();
                                    updatePoint.X = chi.Rect1.X + chi.Rect1.Width / 2;
                                    updatePoint.Y = chi.Rect1.Y + chi.Rect1.Height / 2;
                                    canLin.X1 = chi.Line.X1 = updatePoint.X;
                                    canLin.Y1 = chi.Line.Y1 = updatePoint.Y;
                                }

                                if (chi.Rect2.NodeName == item.Name)
                                {
                                    canLin.X2 = chi.Line.X2 = newLeft + item.ActualWidth / 2;
                                    canLin.Y2 = chi.Line.Y2 = newTop + item.ActualHeight / 2;

                                }
                            }
                        }
                    }

                    index++;
                }
                updateGraphCoord(systemGraph);
            }
        }

        private void ex_B_G_MouseMove(object sender, MouseEventArgs e)
        {
            TextBlock item = sender as TextBlock;
            if (isMouseCaptured)
            {
                Point originalPoint = new Point((double)item.GetValue(Canvas.LeftProperty), (double)item.GetValue(Canvas.TopProperty));

                // Calculate the current position of the object.
                double deltaV = e.GetPosition(null).Y - mouseVerticalPosition;
                double deltaH = e.GetPosition(null).X - mouseHorizontalPosition;
                double newTop = deltaV + (double)item.GetValue(Canvas.TopProperty);
                double newLeft = deltaH + (double)item.GetValue(Canvas.LeftProperty);

                // Set new position of object.
                item.SetValue(Canvas.TopProperty, newTop);
                item.SetValue(Canvas.LeftProperty, newLeft);

                // Update position global variables.
                mouseVerticalPosition = e.GetPosition(null).Y;
                mouseHorizontalPosition = e.GetPosition(null).X;

                //should update the connection lines and so on

                foreach (var elements in rectangles_BG_Simplified)
                {
                    if (elements.NodeName == item.Name)
                    {
                        elements.X = newLeft;
                        elements.Y = newTop;
                    }
                }

                int index = 0;

                foreach (var canvasLines in Graph_BG2.theCanvas.Children)
                {
                    if (canvasLines.GetType().Name == "Path")
                    {
                        var canLin = canvasLines as System.Windows.Shapes.Path;
                        foreach (var chi in connections_BG_Simplified)
                        {
                            if (chi.Line.Name == canLin.Name)
                            {
                                if (chi.Rect1.NodeName == item.Name)
                                {

                                    Point updatePoint = new Point();
                                    Point[] points = new Point[2];
                                    if (chi.Line.ArrowEnd == null || chi.Line.ArrowEnd.Contains("same"))
                                    {
                                        updatePoint.X = chi.Rect1.X + chi.Rect1.Width / 2;
                                        updatePoint.Y = chi.Rect1.Y + chi.Rect1.Height / 2;
                                        chi.Line.X1 = updatePoint.X;
                                        chi.Line.Y1 = updatePoint.Y;

                                        points[0].X = updatePoint.X;
                                        points[0].Y = updatePoint.Y;
                                        points[1].X = chi.Line.X2;
                                        points[1].Y = chi.Line.Y2;
                                    }
                                    else
                                    {
                                        updatePoint.X = chi.Rect1.X + chi.Rect1.Width / 2;
                                        updatePoint.Y = chi.Rect1.Y + chi.Rect1.Height / 2;
                                        chi.Line.X2 = updatePoint.X;
                                        chi.Line.Y2 = updatePoint.Y;

                                        points[0].X = chi.Line.X1;
                                        points[0].Y = chi.Line.Y1;
                                        points[1].X = chi.Line.X2;
                                        points[1].Y = chi.Line.Y2;
                                    }

                                    double new_x, new_y, new_x_1, new_y_1, new_x_2, new_y_2;

                                    vectorComp(points, out new_x, out new_y, out new_x_1, out new_y_1, out new_x_2, out new_y_2);

                                    //access the geometry group in canLin 
                                    LineGeometry blackLineGeometry = new LineGeometry();
                                    blackLineGeometry.StartPoint = new Point(new_x, new_y);
                                    blackLineGeometry.EndPoint = new Point(new_x_1, new_y_1);

                                    LineGeometry blackLineGeometry1 = new LineGeometry();
                                    blackLineGeometry1.StartPoint = new Point(new_x_1, new_y_1);
                                    blackLineGeometry1.EndPoint = new Point(new_x_2, new_y_2);

                                    GeometryGroup blueGeometryGroup = new GeometryGroup();
                                    blueGeometryGroup.Children.Add(blackLineGeometry);
                                    blueGeometryGroup.Children.Add(blackLineGeometry1);

                                    /*  if (causality)
                                      {
                                          Point[] pointss = new Point[2];

                                          pointss[0] = blackLineGeometry.StartPoint;
                                          pointss[1] = blackLineGeometry.EndPoint;

                                          double Cnew_x, Cnew_y, Cnew_x_1, Cnew_y_1;

                                          LineVector(pointss, chi.Line.ArrowEnd, out Cnew_x, out Cnew_y, out Cnew_x_1, out Cnew_y_1);
                                          LineGeometry blackLineGeometry2 = new LineGeometry();
                                          blackLineGeometry2.StartPoint = new Point(Cnew_x, Cnew_y);
                                          blackLineGeometry2.EndPoint = new Point(Cnew_x_1, Cnew_y_1);
                                          blueGeometryGroup.Children.Add(blackLineGeometry2);
                                      } */

                                    canLin.Data = blueGeometryGroup;
                                }

                                if (chi.Rect2.NodeName == item.Name)
                                {
                                    Point[] points = new Point[2];
                                    if (chi.Line.ArrowEnd == null || chi.Line.ArrowEnd.Contains("same"))
                                    {
                                        chi.Line.X2 = newLeft + item.ActualWidth / 2;
                                        chi.Line.Y2 = newTop + item.ActualHeight / 2;

                                        points[0].X = chi.Line.X1;
                                        points[0].Y = chi.Line.Y1;
                                        points[1].X = chi.Line.X2;
                                        points[1].Y = chi.Line.Y2;
                                    }
                                    else
                                    {
                                        chi.Line.X1 = newLeft + item.ActualWidth / 2;
                                        chi.Line.Y1 = newTop + item.ActualHeight / 2;

                                        points[0].X = chi.Line.X1;
                                        points[0].Y = chi.Line.Y1;
                                        points[1].X = chi.Line.X2;
                                        points[1].Y = chi.Line.Y2;
                                    }

                                    double new_x, new_y, new_x_1, new_y_1, new_x_2, new_y_2;

                                    vectorComp(points, out new_x, out new_y, out new_x_1, out new_y_1, out new_x_2, out new_y_2);

                                    //access the geometry group in canLin 
                                    LineGeometry blackLineGeometry = new LineGeometry();
                                    blackLineGeometry.StartPoint = new Point(new_x, new_y);
                                    blackLineGeometry.EndPoint = new Point(new_x_1, new_y_1);

                                    LineGeometry blackLineGeometry1 = new LineGeometry();
                                    blackLineGeometry1.StartPoint = new Point(new_x_1, new_y_1);
                                    blackLineGeometry1.EndPoint = new Point(new_x_2, new_y_2);

                                    GeometryGroup blueGeometryGroup = new GeometryGroup();
                                    blueGeometryGroup.Children.Add(blackLineGeometry);
                                    blueGeometryGroup.Children.Add(blackLineGeometry1);

                                    /*  if (causality)
                                      {
                                          Point[] pointss = new Point[2];

                                          pointss[0] = blackLineGeometry.StartPoint;
                                          pointss[1] = blackLineGeometry.EndPoint;

                                          double Cnew_x, Cnew_y, Cnew_x_1, Cnew_y_1;

                                          LineVector(pointss, chi.Line.ArrowEnd, out Cnew_x, out Cnew_y, out Cnew_x_1, out Cnew_y_1);
                                          LineGeometry blackLineGeometry2 = new LineGeometry();
                                          blackLineGeometry2.StartPoint = new Point(Cnew_x, Cnew_y);
                                          blackLineGeometry2.EndPoint = new Point(Cnew_x_1, Cnew_y_1);
                                          blueGeometryGroup.Children.Add(blackLineGeometry2);
                                      } */

                                    canLin.Data = blueGeometryGroup;
                                }
                            }
                        }
                    }

                    index++;
                }
                //if (selectedOption == -1 && causality == false)
                //    updateGraphCoord_Simplified(systemGraph);
                //else if (selectedOption > -1 && causality == false)
                //  updateGraphCoord_Simplified(sys_Graphs[index1]);
                //else if (selectedOption > -1 && causality)
                //  updateGraphCoord_Simplified(finalresult[index1]);
            }
        }

        private void ex_B_G_C_MouseMove(object sender, MouseEventArgs e)
        {
            TextBlock item = sender as TextBlock;
            if (isMouseCaptured)
            {
                Point originalPoint = new Point((double)item.GetValue(Canvas.LeftProperty), (double)item.GetValue(Canvas.TopProperty));

                // Calculate the current position of the object.
                double deltaV = e.GetPosition(null).Y - mouseVerticalPosition;
                double deltaH = e.GetPosition(null).X - mouseHorizontalPosition;
                double newTop = deltaV + (double)item.GetValue(Canvas.TopProperty);
                double newLeft = deltaH + (double)item.GetValue(Canvas.LeftProperty);

                // Set new position of object.
                item.SetValue(Canvas.TopProperty, newTop);
                item.SetValue(Canvas.LeftProperty, newLeft);

                // Update position global variables.
                mouseVerticalPosition = e.GetPosition(null).Y;
                mouseHorizontalPosition = e.GetPosition(null).X;

                //should update the connection lines and so on

                foreach (var elements in rectangles_BG_Causality)
                {
                    if (elements.NodeName == item.Name)
                    {
                        elements.X = newLeft;
                        elements.Y = newTop;
                    }
                }

                int index = 0;

                foreach (var canvasLines in Graph_BG3.theCanvas.Children)
                {
                    if (canvasLines.GetType().Name == "Path")
                    {
                        var canLin = canvasLines as System.Windows.Shapes.Path;
                        foreach (var chi in connections_BG_Causality)
                        {
                            if (chi.Line.Name == canLin.Name)
                            {
                                if (chi.Rect1.NodeName == item.Name)
                                {

                                    Point updatePoint = new Point();
                                    Point[] points = new Point[2];
                                    if (chi.Line.ArrowEnd == null || chi.Line.ArrowEnd.Contains("same"))
                                    {
                                        updatePoint.X = chi.Rect1.X + chi.Rect1.Width / 2;
                                        updatePoint.Y = chi.Rect1.Y + chi.Rect1.Height / 2;
                                        chi.Line.X1 = updatePoint.X;
                                        chi.Line.Y1 = updatePoint.Y;

                                        points[0].X = updatePoint.X;
                                        points[0].Y = updatePoint.Y;
                                        points[1].X = chi.Line.X2;
                                        points[1].Y = chi.Line.Y2;
                                    }
                                    else
                                    {
                                        updatePoint.X = chi.Rect1.X + chi.Rect1.Width / 2;
                                        updatePoint.Y = chi.Rect1.Y + chi.Rect1.Height / 2;
                                        chi.Line.X2 = updatePoint.X;
                                        chi.Line.Y2 = updatePoint.Y;

                                        points[0].X = chi.Line.X1;
                                        points[0].Y = chi.Line.Y1;
                                        points[1].X = chi.Line.X2;
                                        points[1].Y = chi.Line.Y2;
                                    }

                                    double new_x, new_y, new_x_1, new_y_1, new_x_2, new_y_2;

                                    vectorComp(points, out new_x, out new_y, out new_x_1, out new_y_1, out new_x_2, out new_y_2);

                                    //access the geometry group in canLin 
                                    LineGeometry blackLineGeometry = new LineGeometry();
                                    blackLineGeometry.StartPoint = new Point(new_x, new_y);
                                    blackLineGeometry.EndPoint = new Point(new_x_1, new_y_1);

                                    LineGeometry blackLineGeometry1 = new LineGeometry();
                                    blackLineGeometry1.StartPoint = new Point(new_x_1, new_y_1);
                                    blackLineGeometry1.EndPoint = new Point(new_x_2, new_y_2);

                                    GeometryGroup blueGeometryGroup = new GeometryGroup();
                                    blueGeometryGroup.Children.Add(blackLineGeometry);
                                    blueGeometryGroup.Children.Add(blackLineGeometry1);

                                    if (causality)
                                    {
                                        Point[] pointss = new Point[2];

                                        pointss[0] = blackLineGeometry.StartPoint;
                                        pointss[1] = blackLineGeometry.EndPoint;

                                        double Cnew_x, Cnew_y, Cnew_x_1, Cnew_y_1;

                                        LineVector(pointss, chi.Line.ArrowEnd, out Cnew_x, out Cnew_y, out Cnew_x_1, out Cnew_y_1);
                                        LineGeometry blackLineGeometry2 = new LineGeometry();
                                        blackLineGeometry2.StartPoint = new Point(Cnew_x, Cnew_y);
                                        blackLineGeometry2.EndPoint = new Point(Cnew_x_1, Cnew_y_1);
                                        blueGeometryGroup.Children.Add(blackLineGeometry2);
                                    }

                                    canLin.Data = blueGeometryGroup;
                                }

                                if (chi.Rect2.NodeName == item.Name)
                                {
                                    Point[] points = new Point[2];
                                    if (chi.Line.ArrowEnd == null || chi.Line.ArrowEnd.Contains("same"))
                                    {
                                        chi.Line.X2 = newLeft + item.ActualWidth / 2;
                                        chi.Line.Y2 = newTop + item.ActualHeight / 2;

                                        points[0].X = chi.Line.X1;
                                        points[0].Y = chi.Line.Y1;
                                        points[1].X = chi.Line.X2;
                                        points[1].Y = chi.Line.Y2;
                                    }
                                    else
                                    {
                                        chi.Line.X1 = newLeft + item.ActualWidth / 2;
                                        chi.Line.Y1 = newTop + item.ActualHeight / 2;

                                        points[0].X = chi.Line.X1;
                                        points[0].Y = chi.Line.Y1;
                                        points[1].X = chi.Line.X2;
                                        points[1].Y = chi.Line.Y2;
                                    }

                                    double new_x, new_y, new_x_1, new_y_1, new_x_2, new_y_2;

                                    vectorComp(points, out new_x, out new_y, out new_x_1, out new_y_1, out new_x_2, out new_y_2);

                                    //access the geometry group in canLin 
                                    LineGeometry blackLineGeometry = new LineGeometry();
                                    blackLineGeometry.StartPoint = new Point(new_x, new_y);
                                    blackLineGeometry.EndPoint = new Point(new_x_1, new_y_1);

                                    LineGeometry blackLineGeometry1 = new LineGeometry();
                                    blackLineGeometry1.StartPoint = new Point(new_x_1, new_y_1);
                                    blackLineGeometry1.EndPoint = new Point(new_x_2, new_y_2);

                                    GeometryGroup blueGeometryGroup = new GeometryGroup();
                                    blueGeometryGroup.Children.Add(blackLineGeometry);
                                    blueGeometryGroup.Children.Add(blackLineGeometry1);

                                    if (causality)
                                    {
                                        Point[] pointss = new Point[2];

                                        pointss[0] = blackLineGeometry.StartPoint;
                                        pointss[1] = blackLineGeometry.EndPoint;

                                        double Cnew_x, Cnew_y, Cnew_x_1, Cnew_y_1;

                                        LineVector(pointss, chi.Line.ArrowEnd, out Cnew_x, out Cnew_y, out Cnew_x_1, out Cnew_y_1);
                                        LineGeometry blackLineGeometry2 = new LineGeometry();
                                        blackLineGeometry2.StartPoint = new Point(Cnew_x, Cnew_y);
                                        blackLineGeometry2.EndPoint = new Point(Cnew_x_1, Cnew_y_1);
                                        blueGeometryGroup.Children.Add(blackLineGeometry2);
                                    }

                                    canLin.Data = blueGeometryGroup;
                                }
                            }
                        }
                    }

                    index++;
                }
                // if (selectedOption == -1 && causality == false)
                //     updateGraphCoord_Causality(systemGraph);
                // else if (selectedOption > -1 && causality == false)
                //     updateGraphCoord_Causality(sys_Graphs[selectedOption]);
                // else if (selectedOption > -1 && causality)
                //     updateGraphCoord_Causality(finalresult[selectedOption]);
            }
        }

        private void updateGraphCoord(designGraph systemGraph)
        {
            foreach (node n in systemGraph.nodes)
            {
                foreach (var item in rectangles_BG)
                {
                    if (n.name == item.NodeName)
                    {
                        n.X = item.X;
                        n.Y = item.Y;
                    }
                }
            }
        }

        private void updateGraphCoord_Simplified(designGraph systemGraph)
        {
            foreach (node n in systemGraph.nodes)
            {
                foreach (var item in rectangles_BG_Simplified)
                {
                    if (n.name == item.NodeName)
                    {
                        n.X = item.X;
                        n.Y = item.Y;
                    }
                }
            }
        }

        private void updateGraphCoord_Causality(designGraph systemGraph)
        {
            foreach (node n in systemGraph.nodes)
            {
                foreach (var item in rectangles_BG_Causality)
                {
                    if (n.name == item.NodeName)
                    {
                        n.X = item.X;
                        n.Y = item.Y;
                    }
                }
            }
        }

        private void ex_BG_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBlock item = sender as TextBlock;
            mouseVerticalPosition = e.GetPosition(null).Y;
            mouseHorizontalPosition = e.GetPosition(null).X;
            isMouseCaptured = true;
            item.CaptureMouse();
        }

        private void ex_B_G_C_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBlock item = sender as TextBlock;
            mouseVerticalPosition = e.GetPosition(null).Y;
            mouseHorizontalPosition = e.GetPosition(null).X;
            isMouseCaptured = true;
            item.CaptureMouse();
        }
        private void bondgraphBeforeSimplification()
        {
            options = (systemToBondGraph.recognize(systemGraph, true, null));

            while (options.Count > 0)
            {
                options[0].apply(systemGraph, null);
                options = systemToBondGraph.recognize(systemGraph, true, null);

            }

            List<string> nodeLabels = new List<string>();
            List<int> nodeNames = new List<int>();
            int ll = 0;
            foreach (var no in systemGraph.nodes)
            {
                nodeLabels.Add(String.Join(String.Empty, no.localLabels.ToArray()));
                nodeNames.Add(ll++);
            }
            ll = 0;
            foreach (var opt in systemGraph.nodes)
            {
                opt.name = "name" + nodeNames[ll];
                ll = ll + 1;
            }

            //try to update the positions of each node

            rectangles_BG.Clear();
            lines_BG.Clear();
            connections_BG.Clear();

            layoutAlgorithm();

            foreach (var no in systemGraph.nodes)
            {
                var rect = new RectangleViewModel();

                rect.NodeName = no.name;
                List<string> localLabels = no.localLabels.ToList();

                List<string> localLabels_Copy = no.localLabels.ToList();

                foreach (var uu in localLabels_Copy)
                {
                    if (uu.Contains("vel"))
                        localLabels.Remove(uu);
                    if (uu.Contains("good"))
                        localLabels.Remove(uu);
                    if (uu.Contains("multiple"))
                        localLabels.Remove(uu);
                    if (uu.Contains("system"))
                        localLabels.Remove(uu);
                    if (uu.Contains("iadded"))
                        localLabels.Remove(uu);
                    if (uu.Contains("cadded"))
                        localLabels.Remove(uu);
                    if (uu.Contains("rackadded"))
                        localLabels.Remove(uu);
                    if (uu.Contains("gearadded"))
                        localLabels.Remove(uu);
                    if (uu.Contains("layoutadded"))
                        localLabels.Remove(uu);

                }

                rect.Content = string.Join(" ", localLabels.ToArray());
                rect.X = no.X;
                rect.Y = no.Y;
                rect.Color = "Black";
                rect.Font = "Segoe UI";
                rect.Width = 75;
                rect.Height = 20;
                rectangles_BG.Add(rect);

            }

            //  int p = 101;
            foreach (var no in systemGraph.arcs)
            {
                var line = new LineConnections();

                line.Name = no.name;
                line.LC = "Red";
                line.LTA = "noarrow";
                line.LTD = string.Join(" ", no.localLabels.ToArray());

                line.Thickness = 1;
                line.X1 = no.From.X;
                line.Y1 = no.From.Y;
                line.X2 = no.To.X;
                line.Y2 = no.To.Y;

                var connect = new ConnectionViewModel();

                lines_BG.Add(line);
                connect.Line = line;

                foreach (var rects in rectangles_BG)
                {
                    if (rects.NodeName == no.From.name)
                        connect.Rect1 = rects;
                    if (rects.NodeName == no.To.name)
                        connect.Rect2 = rects;
                }

                connect.ConnectionMultiple = 1;
                connect.ConnectionSide = "Middle";
                connections_BG.Add(connect);
            }

            Dictionary<RectangleViewModel, BondGraphElement> rectElements = new Dictionary<RectangleViewModel, BondGraphElement>();

            // Create all BondGraphElements
            foreach (RectangleViewModel r in rectangles_BG)
            {
                BondGraphElement bgElement = new BondGraphElement(Graph_BG1, r.NodeName, r.Content, new Point(r.X, r.Y));
                rectElements.Add(r, bgElement);
            }

            // Create all BondArcs
            foreach (ConnectionViewModel c in connections_BG)
            {
                BondGraphElement el1 = rectElements[c.Rect1];
                BondGraphElement el2 = rectElements[c.Rect2];
                Arc bgArc = new Arc(el1, el2);
            }

            /*
            List<string> stringLine = new List<string>();
            List<int> indices = new List<int>();

            HashSet<string> rectNames = new HashSet<string>();
            TextBlock ex = null;

            for (int j = 0; j < rectangles_BG.Count; j++)
            {
                int count = 0;
                foreach (var item in connections_BG)
                    if (rectangles_BG[j].NodeName == item.Rect1.NodeName || rectangles_BG[j].NodeName == item.Rect2.NodeName)
                        count++;

                if (count == 0)
                    indices.Add(j);
            }

            if (indices.Count > 0)
            {
                for (int j = 0; j < indices.Count; j++)
                {

                    ex = new TextBlock();
                    ex.Text = rectangles_BG[indices[j]].Content;
                    ex.SetValue(Canvas.TopProperty, rectangles_BG[indices[j]].Y);
                    ex.SetValue(Canvas.LeftProperty, rectangles_BG[indices[j]].X);

                    ex.Visibility = System.Windows.Visibility.Visible;
                    ex.Name = rectangles_BG[indices[j]].NodeName;
                    ex.TextWrapping = TextWrapping.Wrap;
                    ex.FontFamily = new FontFamily(rectangles_BG[indices[j]].Font);
                    ex.Foreground = findColor(rectangles_BG[indices[j]].Color);
                    ex.FontSize = FontSize;

                    ex.MouseLeftButtonDown += new MouseButtonEventHandler(ex_BG_MouseDown);
                    ex.MouseMove += new MouseEventHandler(ex_BG_MouseMove);
                    ex.MouseLeftButtonUp += new MouseButtonEventHandler(ex_BG_MouseUp);
                    ex.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                    ex.Arrange(new Rect(ex.DesiredSize));

                    foreach (var tmp in rectangles_BG)
                        if (tmp.NodeName == ex.Name)
                        {
                            tmp.Width = ex.ActualWidth;
                            tmp.Height = ex.ActualHeight;

                        }
                    Graph_BG1.theCanvas.Children.Add(ex);
                }
            }
            */
        }

        private void layoutAlgorithm()
        {
            //first determine the number of nodes in systemgraphs

            bool checkifelectrical = false;

            foreach (var n in systemGraph.nodes)
                if (n.localLabels.Contains("_Inductor") || n.localLabels.Contains("_Resistor") || n.localLabels.Contains("_Capacitor"))
                {
                    checkifelectrical = true;
                    break;
                }

            if (!checkifelectrical)
            { 
                int L_numberOfNodes = 0;

            foreach (var n in systemGraph.nodes)
                L_numberOfNodes++;

            //size of 1500 x 1500 pixel^2. 
            //size of 100 x 100 

            double squareArea = 1000 * 1000;
            double gridArea = 50 * 50;
            double noOfGrids = squareArea / gridArea;
            //20 x 20 squares
            //coordinate for the center of each square is below 
            double[,] L_coordinateArray_x = new double[20, 20];
            double[,] L_coordinateArray_y = new double[20, 20];

            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    L_coordinateArray_x[i, j] = 50 - i * 50;
                    L_coordinateArray_y[i, j] = 50 - j * 50;
                }
            }

            //got to determine which are at the ends. 
            //first find a 0 or 1 junction

            foreach (var n in systemGraph.nodes)
            {
                if (n.localLabels.Contains("1") || n.localLabels.Contains("0") || n.localLabels.Contains("1GND")) //1GND is anyway at the end. 
                {
                    //now check if this node is connected to more than one 0/1/TF/GY/IGND 

                    L_checkifend(n, out bool checkifend, out bool multiplebranches);

                    if (checkifend)
                        n.localLabels.Add("systemend");
                    if (multiplebranches)
                        n.localLabels.Add("multiplebranches");

                }

                if (n.localLabels.Contains("1GND"))
                    n.localLabels.Add("systemend");

            }


            //now that we have identified the systemends, we can start from one systemend

            //determine the number of systemends in the systemgraph
            int numberofsystemends = 0;
            foreach (var n in systemGraph.nodes)
            {
                if (n.localLabels.Contains("systemend"))
                    numberofsystemends++;

            }


            //the number of system ends have been determined

            //if there are two system ends
            //position them at the two ends - left - right 
            //if three - left, right, top

            if (numberofsystemends == 2)
            {
                List<int> xadded = new List<int>();
                xadded.Add(19);
                xadded.Add(0);
                List<int> yadded = new List<int>();
                yadded.Add(3);
                yadded.Add(3);
                List<int> xonly = new List<int>();
                xonly.Add(19);
                xonly.Add(0);
                List<int> yonly = new List<int>();
                yonly.Add(3);
                yonly.Add(3);
                foreach (var n in systemGraph.nodes)
                {
                    if (n.localLabels.Contains("systemend"))
                    {
                        n.X = L_coordinateArray_x[xonly[0], yonly[0]];
                        n.Y = L_coordinateArray_y[xonly[0], yonly[0]];
                        L_coordinateArray_x[xonly[0], yonly[0]] = 3333.0;
                        L_coordinateArray_y[xonly[0], yonly[0]] = 3333.0;
                        n.localLabels.Add("layoutadded");
                        xonly.RemoveAt(0);
                        yonly.RemoveAt(0);

                    }


                }

                //now determine the number of multiplebranches in the graph
                int numberOfMultiplebranches = 0;
                int numberofPars = 0;
                foreach (var n in systemGraph.nodes)
                {
                    if (n.localLabels.Contains("multiplebranches") && !n.localLabels.Contains("PAR"))
                        numberOfMultiplebranches++;
                    if (n.localLabels.Contains("PAR") && n.localLabels.Contains("multiplebranches"))
                        numberofPars++;
                }

                int divideby2 = numberofPars / 2;

                if (divideby2 > 0)
                {
                    numberOfMultiplebranches = numberOfMultiplebranches - divideby2;
                }

                int distance = 15 / numberOfMultiplebranches;

                //add the layout squares

                xadded.Sort();
                for (int i = 0; i < numberOfMultiplebranches; i++)
                {
                    xonly.Add(xadded.Min() + (i + 1) * distance);
                }
                xonly.Sort();

                //first add nodes that are next to the systemends

                int multibranch = 0;

                foreach (var n in systemGraph.nodes)
                {
                    if (n.localLabels.Contains("systemend") && n.localLabels.Contains("layoutadded"))
                    {
                        int multiplier = -1;
                        //now find if this n.X and n.Y are at which end
                        int xIndexdetermination = 999;

                        for (int i = 0; i < 20; i++)
                        {
                            for (int j = 0; j < 20; j++)
                            {
                                if (n.X == L_coordinateArray_x[i, j])
                                {
                                    xIndexdetermination = i;
                                    break;
                                }
                            }
                        }

                        //now that I have determined the xindex, got to determine if this is max or min

                        bool maxEnd = false;
                        bool minEnd = false;

                        var delta1 = xIndexdetermination - 0;
                        var delta2 = 19 - xIndexdetermination;

                        if (delta2 > delta1)
                        {
                            maxEnd = false;
                            minEnd = true;
                        }
                        else
                        {
                            maxEnd = true;
                            minEnd = false;
                        }
                        foreach (arc n1 in n.arcs)
                        {

                            if (n1.otherNode(n).localLabels.Contains("multiplebranches")
                                &&
                                n1.otherNode(n).localLabels.Contains("PAR"))
                            {


                                if (maxEnd)
                                {
                                    n1.otherNode(n).X = L_coordinateArray_x[xIndexdetermination - distance, yadded[0]];
                                    n1.otherNode(n).Y = n.Y + multiplier * 50 * 2;
                                }
                                else
                                {
                                    n1.otherNode(n).X = L_coordinateArray_x[xIndexdetermination + distance, yadded[0]];
                                    n1.otherNode(n).Y = n.Y + multiplier * 50 * 2;
                                }

                                /* n1.otherNode(n).X = n.X + 50 * distance;
                                 n1.otherNode(n).Y = n.Y+multiplier*50*2;*/
                                multiplier = multiplier * -1;
                                n1.otherNode(n).localLabels.Add("layoutadded");
                            }
                            if (n1.otherNode(n).localLabels.Contains("multiplebranches")
                                &&
                                !n1.otherNode(n).localLabels.Contains("PAR"))
                            {

                                if (maxEnd)
                                {
                                    n1.otherNode(n).X = L_coordinateArray_x[xIndexdetermination - distance, yadded[0]];
                                    n1.otherNode(n).Y = n.Y;
                                }
                                else
                                {
                                    n1.otherNode(n).X = L_coordinateArray_x[xIndexdetermination + distance, yadded[0]];
                                    n1.otherNode(n).Y = n.Y;
                                }
                                n1.otherNode(n).localLabels.Add("layoutadded");
                            }
                        }
                    }



                }

                foreach (var n in systemGraph.nodes)
                {
                    if (n.localLabels.Contains("layoutadded"))
                    {
                        int multiplier = -1;
                        //now find if this n.X and n.Y are at which end
                        int xIndexdetermination = 999;

                        for (int i = 0; i < 20; i++)
                        {
                            for (int j = 0; j < 20; j++)
                            {
                                if (n.X == L_coordinateArray_x[i, j])
                                    xIndexdetermination = i;
                            }
                        }

                        //now that I have determined the xindex, got to determine if this is max or min

                        bool maxEnd = false;
                        bool minEnd = false;


                        //find the delta between xIndex and the ends

                        var delta1 = xIndexdetermination - 0;
                        var delta2 = 19 - xIndexdetermination;

                        if (delta2 > delta1)
                        {
                            maxEnd = false;
                            minEnd = true;
                        }
                        else
                        {
                            maxEnd = true;
                            minEnd = false;
                        }
                        foreach (arc n1 in n.arcs)
                        {

                            if (n1.otherNode(n).localLabels.Contains("multiplebranches")
                                &&
                                n1.otherNode(n).localLabels.Contains("PAR") && !n1.otherNode(n).localLabels.Contains("layoutadded"))
                            {

                                if (maxEnd)
                                {
                                    n1.otherNode(n).X = L_coordinateArray_x[xIndexdetermination - distance, yadded[0]];
                                    n1.otherNode(n).Y = n.Y + multiplier * 50 * 2;
                                }
                                else
                                {
                                    n1.otherNode(n).X = L_coordinateArray_x[xIndexdetermination + distance, yadded[0]];
                                    n1.otherNode(n).Y = n.Y + multiplier * 50 * 2;
                                }

                                /* n1.otherNode(n).X = n.X + 50 * distance;
                                 n1.otherNode(n).Y = n.Y+multiplier*50*2;*/
                                multiplier = multiplier * -1;
                                n1.otherNode(n).localLabels.Add("layoutadded");
                            }
                            if (n1.otherNode(n).localLabels.Contains("multiplebranches")
                                &&
                                !n1.otherNode(n).localLabels.Contains("PAR") && !n1.otherNode(n).localLabels.Contains("layoutadded"))
                            {

                                if (maxEnd)
                                {
                                    n1.otherNode(n).X = L_coordinateArray_x[xIndexdetermination - distance, yadded[0]];
                                    n1.otherNode(n).Y = n.Y;
                                }
                                else
                                {
                                    n1.otherNode(n).X = L_coordinateArray_x[xIndexdetermination + distance, yadded[0]];
                                    n1.otherNode(n).Y = n.Y;
                                }
                                n1.otherNode(n).localLabels.Add("layoutadded");
                            }
                        }
                    }
                }

                //now that all major nodes have been added - now add the I, C, R and sources. 

                foreach (var n in systemGraph.nodes)
                {
                    if (n.localLabels.Contains("layoutadded"))
                    {
                        int numberofnodestoconnect = 0;



                        foreach (arc n1 in n.arcs)
                        {
                            if (!n1.otherNode(n).localLabels.Contains("layoutadded"))
                                numberofnodestoconnect++;
                        }

                        int multiplier = -1;

                        foreach (arc n1 in n.arcs)
                        {
                            if (!n1.otherNode(n).localLabels.Contains("layoutadded"))
                            {
                                n1.otherNode(n).X = n.X;
                                n1.otherNode(n).Y = n.Y + numberofnodestoconnect * multiplier * 50 * 2;
                                multiplier = multiplier * -1;
                                n1.otherNode(n).localLabels.Add("layoutadded");
                                //numberofnodestoconnect--;

                            }
                        }
                    }

                }



            }

        }
        }

        private void L_checkifend(node n, out bool checkifend, out bool multiplebranches)
        {
            int number1 = 0;
            foreach(arc n1 in n.arcs)
            {
                if(n1.otherNode(n).localLabels.Contains("1") || n1.otherNode(n).localLabels.Contains("0")
                    || n1.otherNode(n).localLabels.Contains("TF") || n1.otherNode(n).localLabels.Contains("GY") || 
                    n1.otherNode(n).localLabels.Contains("1GND"))
                {
                    number1++;
                }

            }

            if (number1 == 1)
            {
                checkifend = true;
                multiplebranches = false;//if it's just connected to one other 1/0/TF/GY
            }
            else if(number1>1)
            {
                checkifend = false;
                multiplebranches = true;
            }
            else
            {
                checkifend = false;
                multiplebranches = false;
            }
        }

        public SolidColorBrush findColor(string colorID)
        {
            if (colorID.Contains("Black"))
                return new SolidColorBrush(Colors.Black);
            else if (colorID.Contains("Blue"))
                return new SolidColorBrush(Colors.Blue);
            else if (colorID.Contains("Red"))
                return new SolidColorBrush(Colors.Red);
            else if (colorID.Contains("Orange"))
                return new SolidColorBrush(Colors.Orange);
            else if (colorID.Contains("Green"))
                return new SolidColorBrush(Colors.Green);
            else
                return new SolidColorBrush(Colors.Purple);
        }

        private void generateSystemGraph()
        {
            systemGraph = null;
            StringBuilder builder = new StringBuilder();
            string ruleFileName = "system_graph";

            #region GraphSynth Protocols
            builder.Append("<Page Background=\"#FF000000\" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"").Append(
      " xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\"").Append(
" mc:Ignorable=\"GraphSynth\" xmlns:GraphSynth=\"ignorableUri\" Tag=\"Graph\" ><Border BorderThickness=\"1,1,1,1\"").Append(
          " BorderBrush=\"#FFA9A9A9\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\"><Viewbox ").Append(
" StretchDirection=\"Both\" HorizontalAlignment=\"Stretch\" VerticalAlignment=\"Stretch\"><Canvas Background=\"#FFFFFFFF\"").Append(
         "  Width=\"732.314136125654\" Height=\"570.471204188482\" HorizontalAlignment=\"Stretch\" VerticalAlignment=\"Stretch\"").Append(
         "  RenderTransform=\"1,0,0,-1,0,570.471204188482\"><Ellipse Fill=\"#FF000000\" Tag=\"input\" Width=\"5\" Height=\"5\"").Append(
         "  HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\" /><TextBlock Text=\"input (input, pivot, revolute, ground)\"").Append(
         "  FontSize=\"12\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\" RenderTransform=\"1,0,0,-1,-14.7816666666667,67.175\" />").Append(
              " <Ellipse Fill=\"#FF000000\" Tag=\"ground\" Width=\"5\" Height=\"5\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\" />").Append(
                   " <TextBlock Text=\"ground (ground, link)\" FontSize=\"12\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\"").Append(
         " RenderTransform=\"1,0,0,-1,239.111666666667,67.175\" /><Path Stretch=\"None\" Fill=\"#FF000000\" Stroke=\"#FF000000\"").Append(
          " StrokeThickness=\"1\" StrokeStartLineCap=\"Flat\" StrokeEndLineCap=\"Flat\" StrokeDashCap=\"Flat\" StrokeLineJoin=\"Miter\"").Append(
          " StrokeMiterLimit=\"10\" StrokeDashOffset=\"0\" Tag=\"a0,0,0.5,12:StraightArcController,\" LayoutTransform=\"Identity\"").Append(
          " Margin=\"0,0,0,0\" HorizontalAlignment=\"Stretch\" VerticalAlignment=\"Stretch\" RenderTransform=\"Identity\"").Append(
          " RenderTransformOrigin=\"0,0\" Opacity=\"1\" Visibility=\"Visible\" SnapsToDevicePixels=\"False\"> ").Append(
          " <Path.Data><PathGeometry><PathGeometry.Figures><PathFigure StartPoint=\"77,74.5\" IsFilled=\"False\" IsClosed=\"False\"> ").Append(
          " <PathFigure.Segments><LineSegment Point=\"288,74.5\" /></PathFigure.Segments></PathFigure> ").Append(
          " <PathFigure StartPoint=\"288,74.5\" IsFilled=\"True\" IsClosed=\"True\"><PathFigure.Segments><PolyLineSegment ").Append(
              " Points=\"278,70 281,74.5 278,79\" /></PathFigure.Segments></PathFigure></PathGeometry.Figures></PathGeometry> ").Append(
                  " </Path.Data></Path></Canvas></Viewbox></Border> ").Append(

" <GraphSynth:CanvasProperty BackgroundColor=\"#FFFFFFFF\" AxesColor=\"#FF000000\" AxesOpacity=\"1\" AxesThick=\"0.5\" ").Append(
          " GridColor=\"#FF000000\" GridOpacity=\"1\" GridSpacing=\"24\" GridThick=\"0.25\" SnapToGrid=\"True\"").Append(
          " ScaleFactor=\"1\" ShapeOpacity=\"1\" ZoomToFit=\"False\" ShowNodeName=\"True\" ShowNodeLabel=\"True\"").Append(
          " ShowArcName=\"False\" ShowArcLabel=\"True\" ShowHyperArcName=\"False\" ShowHyperArcLabel=\"True\"").Append(
          " NodeFontSize=\"12\" ArcFontSize=\"12\" HyperArcFontSize=\"12\" NodeTextDistance=\"0\" NodeTextPosition=\"0\"").Append(
          " ArcTextDistance=\"0\" ArcTextPosition=\"0.5\" HyperArcTextDistance=\"0\" HyperArcTextPosition=\"0.5\" GlobalTextSize=\"12\"").Append(
          " CanvasHeight=\"570.47120418848169\" CanvasWidth=\"732.314136125654,732.314136125654,732.314136125654,732.314136125654\"").Append(
          " WindowLeft=\"694.11518324607323\" WindowTop=\"290.5130890052356\" extraAttributes=\"{x:Null}\" Background=\"#FF93CDDD\"").Append(
          " xmlns=\"clr-namespace:GraphSynth.UI;assembly=GraphSynth\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"").Append(
      " xmlns:sx=\"clr-namespace:System.Xml;assembly=System.Xml\" xmlns:av=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"").Append(
       " xmlns:gsui=\"clr-namespace:GraphSynth.UI;assembly=GraphSynth.CustomControls\" xmlns:s=\"clr-namespace:System;assembly=mscorlib\"> ").Append(
          " <CanvasProperty.extraData><x:Array Type=\"sx:XmlElement\"><x:Null /></x:Array></CanvasProperty.extraData> ").Append(
               "</GraphSynth:CanvasProperty>").Append("\n");

            builder.Append("<GraphSynth:designGraph xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" ").Append(
       "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">").Append("\n");

            #endregion 
            builder.AppendLine("<name>" + ruleFileName + "</name>");

            builder.AppendLine("<globalLabels />");
            builder.AppendLine("<globalVariables />");
            int arc1 = 0;
            int name1 = 0;
            if (Graph_System.arcs.Count > 0)
            {
                builder.AppendLine("<arcs>");
                foreach (var item in Graph_System.arcs)
                {
                    string arcname = "arc" + (arc1++);
                    builder.AppendLine("<arc>");
                    builder.AppendLine("<name>" + arcname + "</name>");
                    builder.AppendLine("<localLabels />");


                    builder.AppendLine("<localVariables />");
                    builder.AppendLine("<From>" + item.element1.nodeName + "</From>");
                    builder.AppendLine("<To>" + item.element2.nodeName + "</To>");
                    builder.AppendLine("<directed>false</directed>");
                    builder.AppendLine("<doublyDirected>false</doublyDirected>");

                    builder.AppendLine("</arc>");
                }
                builder.AppendLine("</arcs>");
            }
            else
            {
                builder.AppendLine("<arcs />");
            }
            builder.AppendLine("<nodes>");
            foreach (var item in Graph_System.elements)
            {
                builder.AppendLine("<node>");
                builder.AppendLine("<name>" + item.nodeName + "</name>");
                // char delimit1 = ';';
                builder.AppendLine("<localLabels>");
                foreach (var n in item.labels)
                {
                    {
                        builder.AppendLine("<string>" + n + "</string>");
                    }
                }
                builder.AppendLine("</localLabels>");
                /*  else
                  {

                      builder.AppendLine("<localLabels />");
                  } */

                builder.AppendLine("<localVariables />");

                builder.AppendLine("<X>" + Canvas.GetLeft(item.miniCanvas) + "</X>");
                builder.AppendLine("<Y>" + Canvas.GetTop(item.miniCanvas) + "</Y>");
                builder.AppendLine("<Z>0</Z>");

                builder.AppendLine("</node>");
            }

            builder.AppendLine("</nodes>");

            builder.AppendLine("<hyperarcs />");

            builder.AppendLine("</GraphSynth:designGraph>");
            builder.AppendLine("</Page>");

            XDocument doc_ = XDocument.Parse(builder.ToString());

            XmlReader do1 = doc_.CreateReader();

            var XGraphAndCanvas = XElement.Load(do1);

            var temp2 = XGraphAndCanvas.Element("{ignorableUri}" + "designGraph");
            // var temp = doc_.Element("GraphSynth");

            // if (temp != null)
            var temp = RemoveXAMLns(RemoveIgnorablePrefix(temp2.ToString()));
            {
                var stringReader = new StringReader(temp.ToString());
                var graphDeserializer = new XmlSerializer(typeof(designGraph));

                systemGraph = (designGraph)graphDeserializer.Deserialize(stringReader);
                systemGraph.internallyConnectGraph();
                removeNullWhiteSpaceEmptyLabels(systemGraph);
            }
        }

        // Are we in a situation where we can do most things? Mostly are we NOT in a situation like connecting or dragging elements
        public bool canAlways()
        {
            Graph graph = getActiveGraph();

            return (!graph.connectingMode) && (!graph.draggingMode);
        }

        public bool canDelete()
        {
            if (!canAlways())
                return false;

            Graph graph = getActiveGraph();
            if (!isGraphInteractive(graph))
                return false;
            return (graph.selectedElements.Count > 0) || (graph.selectedArcs.Count > 0);
        }

        public void delete(bool overrideConfirm = false)
        {
            Graph graph = getActiveGraph();
            graph.deleteSelected(overrideConfirm);
        }

        // Clears the graph
        public void newGraph()
        {
            if (unSaved)
            {
                MessageBoxResult result = MessageBox.Show("There are unsaved changes. Do you want to save them?", "Warning", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    save();
                }
                else if (result == MessageBoxResult.No)
                {
                    // Do not save
                }
                else
                {
                    // User hit 'cancel', do not open
                    return;
                }
            }

            Graph_System.clear();
            Graph_BG1.clear();
            Graph_BG2.clear();
            Graph_BG3.clear();

            Graph_BG3.CausalOptions.Items.Clear();
            Graph_BG3.CausalOptions.IsEnabled = false;

            unSaved = false;
            savePath = null;

            graphTabs.SelectedIndex = 0;

            updateCommandButtons();
        }

        public void save()
        {
            if (savePath == null)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "BoGL Graph Files (*.bogl)|*.bogl";
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                // If the user cancels, don't do anything
                if (sfd.ShowDialog() != true)
                    return;

                savePath = sfd.FileName;
            }

            using (StreamWriter sw = new StreamWriter(savePath))
            {
                sw.Write(Graph_System.serialize());
            }

            unSaved = false;
        }

        public void saveAs()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "BoGL Graph Files (*.bogl)|*.bogl";
            sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // If the user cancels, don't do anything
            if (sfd.ShowDialog() != true)
                return;

            savePath = sfd.FileName;

            using (StreamWriter sw = new StreamWriter(savePath))
            {
                sw.Write(Graph_System.serialize());
            }

            unSaved = false;
        }

        // Takes a string and turns it into tokens
        public static List<string> tokenize(string str)
        {
            List<string> tokens = new List<string>();

            // Split the text on newlines
            string[] lines = str.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                string[] lineTokens = line.Trim().Split(" ");
                foreach (string token in lineTokens)
                {
                    if (token.Length > 0)
                        tokens.Add(token);
                }
            }

            return tokens;
        }

        public void open()
        {
            if (unSaved)
            {
                MessageBoxResult result = MessageBox.Show("There are unsaved changes. Do you want to save them?", "Warning", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    save();
                }
                else if (result == MessageBoxResult.No)
                {
                    // Do not save
                }
                else
                {
                    // User hit 'cancel', do not open
                    return;
                }
            }

            // Let user select file
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "BoGL Graph Files (*.bogl)|*.bogl";
            ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (ofd.ShowDialog() != true)
                return;

            // Read file into tokens
            string fileStr = File.ReadAllText(ofd.FileName);

            List<string> fileTokens = tokenize(fileStr);

            // Deserialize graph from tokens
            if (Graph_System.load(fileTokens))
            {
                savePath = ofd.FileName;
                unSaved = false;
            }
            graphTabs.SelectedIndex = 0;

            updateCommandButtons();
        }

        public bool canCutCopy()
        {
            if (!canAlways())
                return false;

            Graph graph = getActiveGraph();
            return isGraphInteractive(graph) && ((graph.selectedElements.Count > 0) || (graph.selectedArcs.Count > 0));
        }

        public void copy()
        {
            Graph graph = getActiveGraph();
            string data = graph.serializeSelected();
            Clipboard.SetData(isGraphInteractive(graph) ? "BoGL System Elements" : "BoGL BondGraph Elements", data);

            updateCommandButtons();
        }

        public void cut()
        {
            copy();
            delete(true);
        }

        public bool canPaste()
        {
            if (!canAlways())
                return false;

            return isGraphInteractive(getActiveGraph()) && Clipboard.ContainsData("BoGL System Elements");
        }

        public void paste()
        {
            Graph graph = getActiveGraph();
            graph.deserializePaste((string)Clipboard.GetData(isGraphInteractive(graph) ? "BoGL System Elements" : "BoGL BondGraph Elements"));
        }

        public bool canUndo()
        {
            if (!canAlways())
                return false;

            Graph graph = getActiveGraph();
            return  isGraphInteractive(graph) && graph.previousState.Length > 0;
        }

        public void undo()
        {
            Graph graph = getActiveGraph();
            String presentState = graph.serialize();
            List<string> tokens = tokenize(graph.previousState);
            graph.load(tokens);
            graph.previousState = "";
            graph.futureState = presentState;

            unSaved = true;

            updateCommandButtons();
        }

        public bool canRedo()
        {
            if (!canAlways())
                return false;

            Graph graph = getActiveGraph();
            return isGraphInteractive(graph) && graph.futureState.Length > 0;
        }

        public void redo()
        {
            Graph graph = getActiveGraph();
            String presentState = graph.serialize();
            List<string> tokens = tokenize(graph.futureState);
            graph.load(tokens);
            graph.previousState = presentState;
            graph.futureState = "";

            unSaved = true;

            updateCommandButtons();
        }

        public void help()
        {
            Hyperlink hyperLink = new Hyperlink()
            {
                NavigateUri = new Uri("https://bogl.mech.website/help/")
            };
            Process.Start(new ProcessStartInfo(hyperLink.NavigateUri.AbsoluteUri) { UseShellExecute = true });
        }

        // Enables/disables buttons based on whether or not they can be executed
        public static void updateCommandButtons()
        {
            MainWindow mw = getInstance();
            bool canDo;

            if (mw.canAlways())
            {
                mw.saveButton.IsHitTestVisible = true;
                mw.saveButton.Opacity = 1;

                canDo = mw.canCutCopy();
                mw.cutButton.IsHitTestVisible = canDo;
                mw.cutButton.Opacity = canDo ? 1 : 0.3;
                mw.copyButton.IsHitTestVisible = canDo;
                mw.copyButton.Opacity = canDo ? 1 : 0.3;

                canDo = mw.canPaste();
                mw.pasteButton.IsHitTestVisible = canDo;
                mw.pasteButton.Opacity = canDo ? 1 : 0.3;

                canDo = mw.canUndo();
                mw.undoButton.IsHitTestVisible = canDo;
                mw.undoButton.Opacity = canDo ? 1 : 0.3;

                canDo = mw.canRedo();
                mw.redoButton.IsHitTestVisible = canDo;
                mw.redoButton.Opacity = canDo ? 1 : 0.3;

                canDo = mw.canDelete();
                mw.deleteButton.IsHitTestVisible = canDo;
                mw.deleteButton.Opacity = canDo ? 1 : 0.3;
            }
            else
            {
                mw.saveButton.IsHitTestVisible = false;
                mw.saveButton.Opacity = 0.3;
                mw.cutButton.IsHitTestVisible = false;
                mw.cutButton.Opacity = 0.3;
                mw.copyButton.IsHitTestVisible = false;
                mw.copyButton.Opacity = 0.3;
                mw.pasteButton.IsHitTestVisible = false;
                mw.pasteButton.Opacity = 0.3;
                mw.undoButton.IsHitTestVisible = false;
                mw.undoButton.Opacity = 0.3;
                mw.redoButton.IsHitTestVisible = false;
                mw.redoButton.Opacity = 0.3;
                mw.deleteButton.IsHitTestVisible = false;
                mw.deleteButton.Opacity = 0.3;
            }
        }

        public void CommandBinding_ExecuteScreenshot(object sender, ExecutedRoutedEventArgs e)
        {
            Graph picturedGraph = getActiveGraph();

            picturedGraph.modifierList.Visibility = Visibility.Hidden;
            picturedGraph.zoomPanel.Visibility = Visibility.Hidden;
            picturedGraph.generateBorder.Visibility = Visibility.Hidden;

            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)picturedGraph.bigCanvas.ActualWidth, (int)picturedGraph.bigCanvas.ActualHeight, 96d, 96d, PixelFormats.Pbgra32);
            renderBitmap.Render(picturedGraph.bigCanvas);
            //JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            picturedGraph.modifierList.Visibility = Visibility.Visible;
            picturedGraph.zoomPanel.Visibility = Visibility.Visible;
            picturedGraph.generateBorder.Visibility = Visibility.Visible;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Image Files|*.png";
            sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // If the user cancels, don't do anything
            if (sfd.ShowDialog() != true)
                return;

            string exportPath = sfd.FileName;

            using (FileStream file = File.Create(exportPath))
            {
                encoder.Save(file);
            }
        }

        public void CommandBinding_ExecuteOpenExample(object sender, ExecutedRoutedEventArgs e)
        {
            if (unSaved)
            {
                MessageBoxResult result = MessageBox.Show("There are unsaved changes. Do you want to save them?", "Warning", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    save();
                }
                else if (result == MessageBoxResult.No)
                {
                    // Do not save
                }
                else
                {
                    // User hit 'cancel', do not open
                    return;
                }
            }

            // Let user select file
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "BoGL Graph Files (*.bogl)|*.bogl";
            ofd.InitialDirectory = Environment.CurrentDirectory + "\\examples";

            if (ofd.ShowDialog() != true)
                return;

            // Read file into tokens
            string fileStr = File.ReadAllText(ofd.FileName);

            List<string> fileTokens = tokenize(fileStr);

            // Deserialize graph from tokens
            if (Graph_System.load(fileTokens))
            {
                savePath = null;
                unSaved = false;
            }
            graphTabs.SelectedIndex = 0;

            updateCommandButtons();
        }

        public void CommandBinding_ExecuteOpenAbout(object sender, ExecutedRoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Show();
        }

        public static MainWindow getInstance()
        {
            return instance;
        }

        private void MainWindow_CanDelete(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = canDelete();
        }

        private void MainWindow_ExecuteDelete(object sender, ExecutedRoutedEventArgs e)
        {
            delete();
        }

        private void MainWindow_CanAlways(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = canAlways();
        }

        private void MainWindow_ExecuteSelectAll(object sender, ExecutedRoutedEventArgs e)
        {
            Graph graph = getActiveGraph();
            graph.selectAll();
        }

        private void MainWindow_ExecuteNew(object sender, ExecutedRoutedEventArgs e)
        {
            newGraph();
        }

        private void MainWindow_ExecuteSave(object sender, ExecutedRoutedEventArgs e)
        {
            save();
        }

        private void MainWindow_ExecuteSaveAs(object sender, ExecutedRoutedEventArgs e)
        {
            saveAs();
        }

        private void MainWindow_ExecuteOpen(object sender, ExecutedRoutedEventArgs e)
        {
            open();
        }

        private void MainWindow_CanCutCopy(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = canCutCopy();
        }

        private void MainWindow_ExecuteCopy(object sender, ExecutedRoutedEventArgs e)
        {
            copy();
        }

        private void MainWindow_ExecuteCut(object sender, ExecutedRoutedEventArgs e)
        {
            cut();
        }

        private void MainWindow_CanPaste(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = canPaste();
        }

        private void MainWindow_ExecutePaste(object sender, ExecutedRoutedEventArgs e)
        {
            paste();
        }

        private void MainWindow_CanUndo(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = canUndo();
        }

        private void MainWindow_ExecuteUndo(object sender, ExecutedRoutedEventArgs e)
        {
            undo();
        }

        private void MainWindow_CanRedo(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = canRedo();
        }

        private void MainWindow_ExecuteRedo(object sender, ExecutedRoutedEventArgs e)
        {
            redo();
        }

        private void MainWindow_ExecuteHelp(object sender, ExecutedRoutedEventArgs e)
        {
            help();
        }

        // Toolbar buttons

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            save();
        }

        private void CutButton_Click(object sender, RoutedEventArgs e)
        {
            if (canCutCopy())
                cut();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (canCutCopy())
                copy();
        }

        private void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (canPaste())
                paste();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (canUndo())
                undo();
        }

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            if (canRedo())
                redo();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Graph graph = getActiveGraph();
            if (isGraphInteractive(graph) && ((graph.selectedElements.Count > 0) || (graph.selectedArcs.Count > 0)))
                delete();
        }

        private void graphTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateCommandButtons();
        }

        // For the Caption Buttons
        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        // Minimize
        private void CommandBinding_Executed_Minimize(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        // Maximize
        private void CommandBinding_Executed_Maximize(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
        }

        // Restore
        private void CommandBinding_Executed_Restore(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
        }

        // Close
        private void CommandBinding_Executed_Close(object sender, ExecutedRoutedEventArgs e)
        {
            if (unSaved)
            {
                MessageBoxResult result = MessageBox.Show("There are unsaved changes. Do you want to save them?", "Warning", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    save();
                    SystemCommands.CloseWindow(this);
                }

                else if (result == MessageBoxResult.No)
                    SystemCommands.CloseWindow(this);

                else
                    return;
            }
            else
                SystemCommands.CloseWindow(this);
        }

        // Opening a link
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        // State change
        private void MainWindowStateChangeRaised(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                MainWindowBorder.BorderThickness = new Thickness(7);
                RestoreButton.Visibility = Visibility.Visible;
                MaximizeButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                MainWindowBorder.BorderThickness = new Thickness(0);
                RestoreButton.Visibility = Visibility.Collapsed;
                MaximizeButton.Visibility = Visibility.Visible;
            }
        }

        private void MainWindow_FocusChanged(object sender, EventArgs e)
        {
            updateCommandButtons();
        }

        private void defineElementModifiers()
        {
            elementModifiers = new Dictionary<String, List<ModifierType>>();
            //mechanical translation init lists
            elementModifiers["System_MT_Mass"] = new List<ModifierType>();
            elementModifiers["System_MT_Spring"] = new List<ModifierType>();
            elementModifiers["System_MT_Damper"] = new List<ModifierType>();
            elementModifiers["System_MT_Ground"] = new List<ModifierType>();
            elementModifiers["System_MT_Force_Input"] = new List<ModifierType>();
            elementModifiers["System_MT_Gravity"] = new List<ModifierType>();
            elementModifiers["System_MT_Velocity_Input"] = new List<ModifierType>();

            //mechanical rotation init lists
            elementModifiers["System_MR_Spring"] = new List<ModifierType>();
            elementModifiers["System_MR_Damper"] = new List<ModifierType>();
            elementModifiers["System_MR_Flywheel"] = new List<ModifierType>();
            elementModifiers["System_MR_Lever"] = new List<ModifierType>();
            elementModifiers["System_MR_Pulley"] = new List<ModifierType>();
            elementModifiers["System_MR_Shaft"] = new List<ModifierType>();
            elementModifiers["System_MR_Rack_Pinion"] = new List<ModifierType>();
            elementModifiers["System_MR_Belt"] = new List<ModifierType>();
            elementModifiers["System_MR_Gear_Pair"] = new List<ModifierType>();
            elementModifiers["System_MR_Torque_Input"] = new List<ModifierType>();
            elementModifiers["System_MR_Pulley_Grounded"] = new List<ModifierType>();
            elementModifiers["System_MR_Velocity_Input"] = new List<ModifierType>();
            elementModifiers["System_MR_Gear"] = new List<ModifierType>();
            elementModifiers["System_MR_Rack"] = new List<ModifierType>();

            //electrical init list 
            elementModifiers["System_E_Capacitor"] = new List<ModifierType>();
            elementModifiers["System_E_Resistor"] = new List<ModifierType>();
            elementModifiers["System_E_Ground"] = new List<ModifierType>();
            elementModifiers["System_E_Transformer"] = new List<ModifierType>();
            elementModifiers["System_E_Junction"] = new List<ModifierType>();
            elementModifiers["System_E_Inductor"] = new List<ModifierType>();
            elementModifiers["System_E_Voltage_Input"] = new List<ModifierType>();
            elementModifiers["System_E_Current_Input"] = new List<ModifierType>();

            //other init lists 
            elementModifiers["System_O_PM_Motor"] = new List<ModifierType>();
            elementModifiers["System_O_VC_Transducer"] = new List<ModifierType>();

            //add modifiers to list 
            //friction 
            elementModifiers["System_MT_Mass"].Add(ModifierType.FRICTION);
            elementModifiers["System_MR_Pulley"].Add(ModifierType.FRICTION);
            elementModifiers["System_MR_Pulley_Grounded"].Add(ModifierType.FRICTION);
            //elementModifiers["System_MR_Rack_Pinion"].Add(ModifierType.FRICTION);
            elementModifiers["System_MR_Gear"].Add(ModifierType.FRICTION);
            elementModifiers["System_MR_Rack"].Add(ModifierType.FRICTION);
            elementModifiers["System_MR_Lever"].Add(ModifierType.FRICTION);

            //parallel
            elementModifiers["System_MT_Spring"].Add(ModifierType.PARALLEL);
            elementModifiers["System_MR_Spring"].Add(ModifierType.PARALLEL);
            elementModifiers["System_MT_Damper"].Add(ModifierType.PARALLEL);
            elementModifiers["System_MR_Damper"].Add(ModifierType.PARALLEL);
            elementModifiers["System_MR_Belt"].Add(ModifierType.PARALLEL);


            //inertia
            elementModifiers["System_MR_Pulley"].Add(ModifierType.INERTIA);
            elementModifiers["System_MR_Pulley_Grounded"].Add(ModifierType.INERTIA);
            elementModifiers["System_MR_Gear"].Add(ModifierType.INERTIA);
            elementModifiers["System_MR_Lever"].Add(ModifierType.INERTIA);
            // elementModifiers["System_MR_Rack_Pinion"].Add(ModifierType.INERTIA);
            elementModifiers["System_MR_Rack"].Add(ModifierType.MASS);

            //toothwear
           // elementModifiers["System_MR_Pulley"].Add(ModifierType.TOOTH_WEAR);
            //elementModifiers["System_MR_Pulley_Grounded"].Add(ModifierType.TOOTH_WEAR);
            elementModifiers["System_MR_Gear"].Add(ModifierType.TOOTH_WEAR);
            //elementModifiers["System_MR_Rack_Pinion"].Add(ModifierType.TOOTH_WEAR);
            elementModifiers["System_MR_Rack"].Add(ModifierType.TOOTH_WEAR);

            //stiffness 
            elementModifiers["System_MR_Shaft"].Add(ModifierType.STIFFNESS);
            elementModifiers["System_MR_Belt"].Add(ModifierType.STIFFNESS);

            //dampening
            elementModifiers["System_MR_Shaft"].Add(ModifierType.DAMPING);
            elementModifiers["System_MR_Belt"].Add(ModifierType.DAMPING);

            //include mass
            elementModifiers["System_MR_Rack"].Add(ModifierType.MASS);

            //velocity
            elementModifiers["System_MT_Mass"].Add(ModifierType.VELOCITY);
            elementModifiers["System_MT_Spring"].Add(ModifierType.VELOCITY);
            elementModifiers["System_MT_Damper"].Add(ModifierType.VELOCITY);
            elementModifiers["System_MT_Force_Input"].Add(ModifierType.VELOCITY);
            elementModifiers["System_MT_Velocity_Input"].Add(ModifierType.VELOCITY);

            elementModifiers["System_MR_Spring"].Add(ModifierType.VELOCITY);
            elementModifiers["System_MR_Damper"].Add(ModifierType.VELOCITY);
            elementModifiers["System_MR_Flywheel"].Add(ModifierType.VELOCITY);
            elementModifiers["System_MR_Lever"].Add(ModifierType.VELOCITY);
            elementModifiers["System_MR_Pulley"].Add(ModifierType.VELOCITY);
            elementModifiers["System_MR_Shaft"].Add(ModifierType.VELOCITY);
            elementModifiers["System_MR_Belt"].Add(ModifierType.VELOCITY);
            elementModifiers["System_MR_Pulley_Grounded"].Add(ModifierType.VELOCITY);
            elementModifiers["System_MR_Gear"].Add(ModifierType.VELOCITY);
            elementModifiers["System_MR_Rack"].Add(ModifierType.VELOCITY);
            elementModifiers["System_MT_Force_Input"].Add(ModifierType.VELOCITY);
            elementModifiers["System_MT_Velocity_Input"].Add(ModifierType.VELOCITY);
            elementModifiers["System_MR_Velocity_Input"].Add(ModifierType.VELOCITY);
            elementModifiers["System_MR_Torque_Input"].Add(ModifierType.VELOCITY);
        }

        private void defineMaxConnections()
        {
            maxConnections = new Dictionary<String, int>();
            //electrical 
            maxConnections["System_E_Capacitor"] = 2;
            maxConnections["System_E_Resistor"] = 2;
            maxConnections["System_E_Inductor"] = 2;
            maxConnections["System_E_Ground"] = 2;
            maxConnections["System_E_Transformer"] = 4;
            maxConnections["System_E_Junction"] = 4;
            //mechanical translation

            maxConnections["System_MT_Spring"] = 2;
            maxConnections["System_MT_Damper"] = 2;

            //mechanical rotation 
            maxConnections["System_MR_Spring"] = 2;
            maxConnections["System_MR_Damper"] = 2;
            maxConnections["System_MR_Shaft"] = 2;
            //other
        }

        private void connectElements(string element1, string element2)
        {
            if (!elementCompatibility[element1].Contains(element2))
                elementCompatibility[element1].Add(element2);
            if (!elementCompatibility[element2].Contains(element1))
                elementCompatibility[element2].Add(element1);
        }

        private void defineElementCompatibility()
        {
            // Set up the blank data structure
            elementCompatibility = new Dictionary<string, List<string>>();

            List<string> allMT = new List<string>();
            allMT.Add("System_MT_Mass");
            allMT.Add("System_MT_Spring");
            allMT.Add("System_MT_Damper");
            allMT.Add("System_MT_Ground");
            allMT.Add("System_MT_Force_Input");
            allMT.Add("System_MT_Gravity");
            allMT.Add("System_MT_Velocity_Input");

            allMT.ForEach(elementName =>
            {
                elementCompatibility[elementName] = new List<string>();
            });

            List<string> allMR = new List<string>();
            allMR.Add("System_MR_Spring");
            allMR.Add("System_MR_Damper");
            allMR.Add("System_MR_Flywheel");
            allMR.Add("System_MR_Lever");
            allMR.Add("System_MR_Pulley");
            allMR.Add("System_MR_Shaft");
            allMR.Add("System_MR_Belt");
            allMR.Add("System_MR_Torque_Input");
            allMR.Add("System_MR_Pulley_Grounded");
            allMR.Add("System_MR_Velocity_Input");
            allMR.Add("System_MR_Gear");
            allMR.Add("System_MR_Rack");

            allMR.ForEach(elementName =>
            {
                elementCompatibility[elementName] = new List<string>();
            });

            List<string> allE = new List<string>();
            allE.Add("System_E_Capacitor");
            allE.Add("System_E_Resistor");
            allE.Add("System_E_Ground");
            allE.Add("System_E_Transformer");
            allE.Add("System_E_Junction");
            allE.Add("System_E_Inductor");
            allE.Add("System_E_Voltage_Input");
            allE.Add("System_E_Current_Input");

            allE.ForEach(elementName =>
            {
                elementCompatibility[elementName] = new List<string>();
            });

            List<string> allO = new List<string>();
            allO.Add("System_O_VC_Transducer");
            allO.Add("System_O_PM_Motor");

            allO.ForEach(elementName =>
            {
                elementCompatibility[elementName] = new List<string>();
            });

            // Connect elements
            allMT.Add("System_MR_Rack");
            allMT.Add("System_MR_Lever");
            allMT.Add("System_MR_Pulley");
            allMT.Add("System_MR_Belt");
            allMT.ForEach(element1 =>
            {
                allMT.ForEach(element2 =>
                {
                    connectElements(element1, element2);
                });
            });

            allMR.Add("System_O_PM_Motor");
            allMR.ForEach(element1 =>
            {
                allMR.ForEach(element2 =>
                {
                    connectElements(element1, element2);
                });
            });

            allE.Add("System_O_PM_Motor");
            allE.ForEach(element1 =>
            {
                allE.ForEach(element2 =>
                {
                    connectElements(element1, element2);
                });
            });

            allO.ForEach(element1 =>
            {
                allO.ForEach(element2 =>
                {
                    connectElements(element1, element2);
                });
            });
        }

        private void MainWindow_ExecuteReportBugs(object sender, ExecutedRoutedEventArgs e)
        {
            openbugform();
        }

        private void openbugform()
        {
            Hyperlink hyperLink = new Hyperlink()
            {
                NavigateUri = new Uri("https://docs.google.com/forms/d/e/1FAIpQLSffPWzycTP4QXjOFTU0VCcUNcLwqEurq5vl44EDE-OqM7jqzQ/viewform")
            };
            Process.Start(new ProcessStartInfo(hyperLink.NavigateUri.AbsoluteUri) { UseShellExecute = true });
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            openbugform();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            string selectedexample = ((MenuItem)sender).Header.ToString();
            string filename = "";
           //"Basic Two Mass System - 1" 
            //"Basic Two Mass System - 2" 
            //"Basic Two Mass System - 3" 
            // "Quarter Car Model" 
            //"Basic Mass, Spring System with Directions" 
            //"Spring and Damper with Force" 

            if(selectedexample.Contains("Basic Two Mass System - 1"))
                filename= "AVL_Prototype_1.examples.MechanicalTranslation.basic-two-mass-system.bogl";
            else if(selectedexample.Contains("Basic Two Mass System - 2"))
                filename = "AVL_Prototype_1.examples.MechanicalTranslation.basic-two-mass-system1.bogl";
            else if (selectedexample.Contains("Basic Two Mass System - 3"))
                filename = "AVL_Prototype_1.examples.MechanicalTranslation.basic-two-mass-system2.bogl";
            else if (selectedexample.Contains("Quarter Car Model"))
                filename = "AVL_Prototype_1.examples.MechanicalTranslation.masses_on_a_spring.bogl";
            else if (selectedexample.Contains("Basic Mass, Spring System with Directions"))
                filename = "AVL_Prototype_1.examples.MechanicalTranslation.moving_masses.bogl";
            else if (selectedexample.Contains("Spring and Damper with Force"))
                filename = "AVL_Prototype_1.examples.MechanicalTranslation.spring_&_damper.bogl";
            else if (selectedexample.Contains("Rack and Pinion System"))
                filename = "AVL_Prototype_1.examples.MechanicalRotation.rack_pinion.bogl";
            else if (selectedexample.Contains("Motor, Shaft and Gear-Pair"))
                filename = "AVL_Prototype_1.examples.MechanicalRotation.motor-gear-pair.bogl";
            else if (selectedexample.Contains("L R C Circuit"))
                filename = "AVL_Prototype_1.examples.Electrical.lrc_circuit.bogl";


            var assembly = Assembly.GetExecutingAssembly();
            //filename = "AVL_Prototype_1.examples.MechanicalTranslation.basic-two-mass-system.bogl";
            Stream stream = assembly.GetManifestResourceStream(filename);
            StreamReader reader = new StreamReader(stream);
            string text = reader.ReadToEnd();


            openexamplefile(text);
        }

        private void openexamplefile(string text)
        {
            List<string> fileTokens = tokenize(text);

            // Deserialize graph from tokens
            if (Graph_System.load(fileTokens))
            {
               // savePath = ofd.FileName;
                unSaved = false;
            }
            graphTabs.SelectedIndex = 0;

            updateCommandButtons();
        }
    }

    // Taken from https://stackoverflow.com/questions/1055670/deactivate-focusvisualstyle-globally
    public class FocusVisualStyleRemover
    {
        static FocusVisualStyleRemover()
        {
            EventManager.RegisterClassHandler(typeof(FrameworkElement), FrameworkElement.GotFocusEvent, new RoutedEventHandler(RemoveFocusVisualStyle), true);
        }

        public static void Init()
        {
            // intentially empty
        }

        private static void RemoveFocusVisualStyle(object sender, RoutedEventArgs e)
        {
            (sender as FrameworkElement).FocusVisualStyle = null;
        }
    }

    //for holding custom commands
    public static class CustomCommands
    {
        public static readonly RoutedUICommand SaveAsImage = new RoutedUICommand("SaveAsImage", "SaveAsImage", typeof(MainWindow));
        public static readonly RoutedUICommand OpenExampleFile = new RoutedUICommand("OpenExampleFile", "OpenExampleFile", typeof(MainWindow));
        public static readonly RoutedUICommand OpenAbout = new RoutedUICommand("OpenAbout", "OpenAbout", typeof(MainWindow));
    }
}
