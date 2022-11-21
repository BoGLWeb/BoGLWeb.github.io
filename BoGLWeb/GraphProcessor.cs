using AVL_Prototype_1;
using GraphSynth.Representation;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using static AVL_Prototype_1.Graph;
using static System.Net.Mime.MediaTypeNames;

namespace BoGLWeb {
    public class BondGraphFactory {

        /// <summary>
        /// Creates a set of bondgraphs from a designGraph
        /// </summary>
        /// <param name="systemGraph">The design graph to process</param>
        /// <returns>First element of tuple is unsimplifiedBG, second is simplifiedBG, last is list of causalBGs</returns>
        /// <exception cref="Exception">Thrown if the any generated BondGraphs are null</exception>
        public static (BondGraph, BondGraph, List<BondGraph>) generateBondGraphs(designGraph systemGraph) {
            BondGraphFactory factory = new(systemGraph);

            if (factory.simplifiedBG is null || factory.unsimplifiedBG is null) {
                //TODO Need to figure out what this error means and figure out how to show it to the user
                //TODO Might want to split these to make error messages clearer
                throw new Exception("simplifiedBG or unsimplifiedBG is null");
            }

            BondGraph unsimplified = BondGraph.generateBondGraphFromGraphSynth(factory.unsimplifiedBG);
            BondGraph simplified = BondGraph.generateBondGraphFromGraphSynth(factory.simplifiedBG);

            List<BondGraph> causalBGs = new();
            foreach (var graph in factory.finalresult) {
                causalBGs.Add(BondGraph.generateBondGraphFromGraphSynth(graph));
            }

            return (unsimplified, simplified, causalBGs);
        }

        private readonly designGraph systemGraph;

        private readonly List<designGraph> optiGraphs;
        private readonly List<int> indiceswithoutINVD;
        private readonly List<int> maxIntegralCausality;

        //I think these are our causal BGs
        private readonly List<designGraph> finalresult;

        private readonly HashSet<string> nodeLabelSorted;
        private readonly HashSet<int> sortedIndices;

        private readonly List<designGraph> sys_Graphs;

        private List<option> options;

        private designGraph? unsimplifiedBG;
        private designGraph? simplifiedBG;
        private readonly List<designGraph> causalBGs;

        private BondGraphFactory(designGraph systemGraph) {
            this.systemGraph = systemGraph;
            this.optiGraphs = new List<designGraph>();
            this.indiceswithoutINVD = new List<int>();
            this.maxIntegralCausality = new List<int>();
            this.finalresult = new List<designGraph>();
            this.nodeLabelSorted = new HashSet<string>();
            this.sortedIndices = new HashSet<int>();
            this.sys_Graphs = new List<designGraph>();
            this.options = new List<option>();

            this.unsimplifiedBG = null;
            this.simplifiedBG = null;
            this.causalBGs = new List<designGraph>();

            generateBondGraph();
        }

        //Mainly code from desktop BoGL
        //TODO cleanup variable names and old comments
        private void generateBondGraph() {
            //now remove all the labels that we added
            //need to return bool value if vel directions are fine or not. 
            //assigning I: and C: nodes with some identifier

            // bool noGood = true;
            checkIfVelocityDirectionsAreOkay(out bool noGood);

            if (!noGood) {
                //TODO Find a better way to display this error message
                Console.WriteLine("Velocity directions are an issue. If this continues, please delete velocity directions and try again. Thanks!");
            } else {
                bondgraphBeforeSimplification();
                bondgraphSimplified();

                int ii = 0;
                foreach (var n in systemGraph.nodes) {

                    if (n.localLabels.Contains("I:"))

                        n.localLabels.Add("iadded" + (ii.ToString()));
                    if (n.localLabels.Contains("C:")
                        )
                        n.localLabels.Add("cadded" + (ii.ToString()));
                    ii++;
                }
                obtainCausality();
            }
        }

        private void obtainCausality() {
            causalBGs.Clear();
            nodeLabelSorted.Clear();
            sortedIndices.Clear();

            finalresult.Clear();
            optiGraphs.Clear();

            //will just do one option for now, will figure out 

            #region initial causality
            Stack<designGraph> sysGraphs = new();

            sysGraphs.Push(systemGraph.copy());
            var sys = sysGraphs.Pop();
            options = RuleSetMap.getInstance().getRuleSet("NewCausalityMethodRuleset").recognize(sys, false, null);
            while (options.Count > 0) {
                options[0].apply(sys, null);
                options = RuleSetMap.getInstance().getRuleSet("NewCausalityMethodRuleset").recognize(sys, false, null);
            }

            sysGraphs.Push(sys);
            while (sysGraphs.Count > 0) {
                sys = sysGraphs.Pop();
                options = RuleSetMap.getInstance().getRuleSet("NewCausalityMethodRuleset_2").recognize(sys, false, null);
                if (options.Count > 0) {
                    foreach (var opt in options) {
                        var gra = sys.copy();
                        GraphSynth.Search.SearchProcess.transferLmappingToChild(gra, sys, opt.nodes, opt.arcs, opt.hyperarcs);
                        opt.apply(gra, null);
                        sysGraphs.Push(gra);
                    }
                } else
                    sys_Graphs.Add(sys);
            }

            foreach (var item in sys_Graphs) {
                sys = item.copy();
                options = RuleSetMap.getInstance().getRuleSet("NewCausalityMethodRuleset_3").recognize(sys, false, null);

                while (options.Count > 0) {
                    options[0].apply(sys, null);
                    options = RuleSetMap.getInstance().getRuleSet("NewCausalityMethodRuleset_3").recognize(sys, false, null);
                }
                sysGraphs.Push(sys);

            }

            sys_Graphs.Clear();

            #endregion 


            while (sysGraphs.Count > 0) {
                sys = sysGraphs.Pop();
                options = RuleSetMap.getInstance().getRuleSet("INVDMarkerRules").recognize(sys, false, null);
                while (options.Count > 0) {
                    options[0].apply(sys, null);
                    options = RuleSetMap.getInstance().getRuleSet("INVDMarkerRules").recognize(sys, false, null);
                }

                Stack<designGraph> graphss = new();
                List<designGraph> graph_SSS = new();
                graphss.Push(sys);

                {
                    var graphS = graphss.Pop();
                    var options1 = RuleSetMap.getInstance().getRuleSet("CalibrationNewRuleset").recognize(graphS, false, null);

                    if (options1.Count > 0) {
                        foreach (var opt in options1) {
                            var graphSS = graphS.copy();
                            GraphSynth.Search.SearchProcess.transferLmappingToChild(graphSS, graphS, opt.nodes,
                                                               opt.arcs, opt.hyperarcs);
                            opt.apply(graphSS, null);
                            graph_SSS.Add(graphSS);
                        }
                    } else
                        graph_SSS.Add(graphS);

                }

                foreach (var opt in graph_SSS) {
                    var options1 = RuleSetMap.getInstance().getRuleSet("CalibrationNewRuleset").recognize(opt, false, null);

                    while (options1.Count > 0) {
                        options1[0].apply(opt, null);
                        options1 = RuleSetMap.getInstance().getRuleSet("CalibrationNewRuleset").recognize(opt, false, null);
                    }

                    graphss.Push(opt);
                }

                graph_SSS.Clear();

                while (graphss.Count > 0) {
                    var graphS = graphss.Pop();
                    var options1 = RuleSetMap.getInstance().getRuleSet("RFlagCleanRuleset").recognize(graphS, false, null);

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
                    while (options1.Count > 0) {
                        options1[0].apply(graphS, null);
                        options1 = RuleSetMap.getInstance().getRuleSet("RFlagCleanRuleset").recognize(graphS, false, null);
                    }
                    graph_SSS.Add(graphS);
                }
                foreach (var opt in graph_SSS) {
                    graphss.Push(opt);
                }

                graph_SSS.Clear();

                while (graphss.Count > 0) {
                    var graphS = graphss.Pop();
                    var options1 = RuleSetMap.getInstance().getRuleSet("ICFixTotalRuleset").recognize(graphS, false, null);

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

                    while (options1.Count > 0) {
                        options1[0].apply(graphS, null);
                        options1 = RuleSetMap.getInstance().getRuleSet("ICFixTotalRuleset").recognize(graphS, false, null);
                    }

                    graph_SSS.Add(graphS);
                }

                foreach (var op in graph_SSS) {
                    options = RuleSetMap.getInstance().getRuleSet("TransformerFlipRuleset").recognize(op, false, null);

                    if (options.Count > 0) {
                        options[0].apply(op, null);
                        sysGraphs.Push(op);
                    } else {


                        options = RuleSetMap.getInstance().getRuleSet("TransformerFlipRuleset2").recognize(op, false, null);

                        if (options.Count > 0) {

                            options[0].apply(op, null);
                            sysGraphs.Push(op);
                        } else
                            optiGraphs.Add(op);
                    }


                }
            }

            foreach (var opt in optiGraphs) {
                options = RuleSetMap.getInstance().getRuleSet("Clean23Ruleset").recognize(opt, false, null);

                while (options.Count > 0) {
                    options[0].apply(opt, null);
                    options = RuleSetMap.getInstance().getRuleSet("Clean23Ruleset").recognize(opt, false, null);
                }

                finalresult.Add(opt);
            }

            indiceswithoutINVD.Clear();
            maxIntegralCausality.Clear();
            for (int ii = 0; ii < finalresult.Count; ii++) {
                bool index = checkINVD(finalresult[ii]);

                if (index == false) {
                    indiceswithoutINVD.Add(ii);
                    maxIntegralCausality.Add(checkICs(finalresult[ii]));

                }
            }

            //now from the list of finalgraph, eliminate duplicate solutions



            if (indiceswithoutINVD.Count == 0) {
                Console.WriteLine("Sorry, we have encountered an error with respect to Causality assignment");

            }

            //need to add exception here if the program is unable to added 

            else {
                int currentHashSetcount = 0;
                int nn = 0;
                foreach (var n in indiceswithoutINVD) {
                    List<string> nodeLabels_Cau = new();
                    var cauGraph = finalresult[n];

                    foreach (var arcC in cauGraph.arcs) {
                        if (arcC.localLabels.Contains("I2") && arcC.localLabels.Contains("SAME")) {
                            for (int iii = 0; iii < arcC.To.localLabels.Count; iii++) {
                                if (arcC.To.localLabels[iii].Contains("iadded")) {
                                    nodeLabels_Cau.Add(arcC.To.localLabels[iii]);

                                }

                            }
                        }
                        if (arcC.localLabels.Contains("C3") && arcC.localLabels.Contains("OPP")) {
                            for (int iii = 0; iii < arcC.From.localLabels.Count; iii++) {
                                if (arcC.From.localLabels[iii].Contains("cadded")) {
                                    nodeLabels_Cau.Add(arcC.From.localLabels[iii]);

                                }

                            }
                        }

                    }

                    if (nodeLabels_Cau.Count > 0) {
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

                        if (currentHashSetcount <= nodeLabelSorted.Count) {
                            sortedIndices.Add(n);
                            currentHashSetcount++;
                        }

                    }
                    nn++;
                }
                // nodeNames_Cau.Sort();
                // nodeNames.Add(nodeNames_Cau);

                List<int> maxIntegralCaus = new();

                foreach (var n in sortedIndices) {
                    //use the indiceswithoutINVD to obtain the index in that list

                    var indexindex = indiceswithoutINVD.FindIndex(item => item == n);
                    maxIntegralCaus.Add(maxIntegralCausality[indexindex]);

                }

                //  foreach (var no in finalresult[index1].nodes)

                //now add to the combo-box

                causalBGs.Clear();

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
        }

        private void bondgraphBeforeSimplification() {
            options = RuleSetMap.getInstance().getRuleSet("systemToBondGraph").recognize(systemGraph, true, null);

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = RuleSetMap.getInstance().getRuleSet("systemToBondGraph").recognize(systemGraph, true, null);

            }

            List<string> nodeLabels = new();
            List<int> list = new();
            List<int> nodeNames = list;
            int ll = 0;
            foreach (var no in systemGraph.nodes) {
                nodeLabels.Add(String.Join(String.Empty, no.localLabels.ToArray()));
                nodeNames.Add(ll++);
            }
            ll = 0;
            foreach (var opt in systemGraph.nodes) {
                opt.name = "name" + nodeNames[ll];
                ll++;
            }

            //try to update the positions of each node

            unsimplifiedBG = systemGraph.copy();

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

        private void bondgraphSimplified() {
            options = RuleSetMap.getInstance().getRuleSet("SimplificationRuleset").recognize(systemGraph, false, null);

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = RuleSetMap.getInstance().getRuleSet("SimplificationRuleset").recognize(systemGraph, false, null);
            }

            options = RuleSetMap.getInstance().getRuleSet("directionRuleSet").recognize(systemGraph, false, null);

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = RuleSetMap.getInstance().getRuleSet("directionRuleSet").recognize(systemGraph, false, null);
            }

            options = RuleSetMap.getInstance().getRuleSet("directionRuleSet2").recognize(systemGraph, false, null);

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = RuleSetMap.getInstance().getRuleSet("directionRuleSet2").recognize(systemGraph, false, null);
            }

            options = RuleSetMap.getInstance().getRuleSet("directionRuleSet3").recognize(systemGraph, false, null);

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = RuleSetMap.getInstance().getRuleSet("directionRuleSet3").recognize(systemGraph, false, null);
            }

            options = RuleSetMap.getInstance().getRuleSet("SimplificationRuleset2").recognize(systemGraph, false, null);

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = RuleSetMap.getInstance().getRuleSet("SimplificationRuleset2").recognize(systemGraph, false, null);
            }

            //again do a deepcopy of the systemGraph 

            //  designGraph SimplifiedGraphWithDir = systemGraph.copy(true);

            simplifiedBG = systemGraph.copy();

            //now let us apply directions - first let us do the sources and i-c-r 

            //all sources should have arrow heads away from source 
            //all I-C-R should have arrow heads into them. 

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

        private void checkIfVelocityDirectionsAreOkay(out bool noGood) {
            //works for mechanical translation systems only - no mechanical rotation yet

            //first load the verify direction rules 

            foreach (var item in RuleSetMap.getInstance().getRuleSet("VerifyBGDir").rules) {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            //second step is to verify if all nodes have velocity directions 

            options = RuleSetMap.getInstance().getRuleSet("VerifyBGDir").recognize(systemGraph, false, null);

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = RuleSetMap.getInstance().getRuleSet("SimplificationRuleset").recognize(systemGraph, false, null);
            }

            //now check if all the nodes that have veladded label have good label as well

            noGood = true;

            foreach (var n in systemGraph.nodes) {
                if (n.localLabels.Contains("veladded") && !n.localLabels.Contains("good")) {
                    noGood = false;
                    break;
                }
            }
        }

        private static int checkICs(designGraph designGraph) {
            int xx = 0;
            foreach (arc a in designGraph.arcs) {
                if (a.localLabels.Contains("I2") && a.localLabels.Contains("SAME"))
                    xx++;
                if (a.localLabels.Contains("C3") && a.localLabels.Contains("OPP"))
                    xx++;
            }

            return (xx);

        }

        private static bool checkINVD(designGraph designGraph) {
            foreach (node n in designGraph.nodes) {
                foreach (string x in n.localLabels) {
                    if (x.Contains("INVD"))
                        return true;
                    if (x.Contains("Flipped"))
                        return true;
                }

            }
            return false;
        }

    }

}

