using AVL_Prototype_1;
using GraphSynth.Representation;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using static AVL_Prototype_1.Graph;
using static System.Net.Mime.MediaTypeNames;

namespace BoGLWeb {
    public class BondGraphFactory {

        //Returns a list. First element is the Unsimplified BG,
        //Second is the Simplified BG and rest are the Causal BGs
        public static List<BondGraph> generateBondGraphs(designGraph systemGraph) {
            BondGraphFactory factory = new BondGraphFactory(systemGraph);

            return null;
        }

        private designGraph systemGraph;

        private List<designGraph> optiGraphs;
        private List<int> indiceswithoutINVD;
        private List<int> maxIntegralCausality;
        private List<designGraph> finalresult;

        private HashSet<string> nodeLabelStorted;
        private HashSet<int> sortedIndices;

        private List<designGraph> sys_Graphs;

        private List<option> options;

        private BondGraphFactory(designGraph systemGraph) {
            this.systemGraph = systemGraph;
            this.optiGraphs = new List<designGraph>();
            this.indiceswithoutINVD = new List<int>();
            this.maxIntegralCausality = new List<int>();
            this.finalresult = new List<designGraph>();
            this.nodeLabelStorted = new HashSet<string>();
            this.sortedIndices = new HashSet<int>();
            this.sys_Graphs = new List<designGraph>();
            this.options = new List<option>();
        }

        public void generateBondGraph() {
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
                options = (INVDMarkerRules.recognize(sys, false, null));
                while (options.Count > 0) {
                    options[0].apply(sys, null);
                    options = INVDMarkerRules.recognize(sys, false, null);
                }

                Stack<designGraph> graphss = new Stack<designGraph>();
                List<designGraph> graph_SSS = new List<designGraph>();
                graphss.Push(sys);

                {
                    var graphS = graphss.Pop();
                    var options1 = (CalibrationNewRuleset.recognize(graphS, false, null));

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
                    var options1 = (CalibrationNewRuleset.recognize(opt, false, null));

                    while (options1.Count > 0) {
                        options1[0].apply(opt, null);
                        options1 = CalibrationNewRuleset.recognize(opt, false, null);
                    }

                    graphss.Push(opt);
                }

                graph_SSS.Clear();

                while (graphss.Count > 0) {
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
                    while (options1.Count > 0) {
                        options1[0].apply(graphS, null);
                        options1 = (RFlagCleanRuleset.recognize(graphS, false, null));
                    }
                    graph_SSS.Add(graphS);
                }
                foreach (var opt in graph_SSS) {
                    graphss.Push(opt);
                }

                graph_SSS.Clear();

                while (graphss.Count > 0) {
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

                    while (options1.Count > 0) {
                        options1[0].apply(graphS, null);
                        options1 = (ICFixTotalRuleset.recognize(graphS, false, null));
                    }

                    graph_SSS.Add(graphS);
                }

                foreach (var op in graph_SSS) {
                    options = (TransformerFlipRuleset.recognize(op, false, null));

                    if (options.Count > 0) {
                        options[0].apply(op, null);
                        sysGraphs.Push(op);
                    } else {


                        options = (TransformerFlipRuleset2.recognize(op, false, null));

                        if (options.Count > 0) {

                            options[0].apply(op, null);
                            sysGraphs.Push(op);
                        } else
                            optiGraphs.Add(op);
                    }


                }
            }

            foreach (var opt in optiGraphs) {
                options = (Clean23Ruleset.recognize(opt, false, null));

                while (options.Count > 0) {
                    options[0].apply(opt, null);
                    options = Clean23Ruleset.recognize(opt, false, null);
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
                MessageBox.Show("Sorry, we have encountered an error with respect to Causality assignment");

            }

            //need to add exception here if the program is unable to added 

            else {
                int currentHashSetcount = 0;
                int nn = 0;
                foreach (var n in indiceswithoutINVD) {
                    List<string> nodeLabels_Cau = new List<string>();
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

                List<int> maxIntegralCaus = new List<int>();

                foreach (var n in sortedIndices) {
                    //use the indiceswithoutINVD to obtain the index in that list

                    var indexindex = indiceswithoutINVD.FindIndex(item => item == n);
                    maxIntegralCaus.Add(maxIntegralCausality[indexindex]);

                }

                index1 = maxIntegralCausality.IndexOf(maxIntegralCausality.Max());

                //  foreach (var no in finalresult[index1].nodes)

                //now add to the combo-box

                for (int pp = 0; pp < nodeLabelSorted.Count; pp++) {
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

        private void bondgraphBeforeSimplification() {
            options = (systemToBondGraph.recognize(systemGraph, true, null));

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = systemToBondGraph.recognize(systemGraph, true, null);

            }

            List<string> nodeLabels = new List<string>();
            List<int> nodeNames = new List<int>();
            int ll = 0;
            foreach (var no in systemGraph.nodes) {
                nodeLabels.Add(String.Join(String.Empty, no.localLabels.ToArray()));
                nodeNames.Add(ll++);
            }
            ll = 0;
            foreach (var opt in systemGraph.nodes) {
                opt.name = "name" + nodeNames[ll];
                ll = ll + 1;
            }

            //try to update the positions of each node

            rectangles_BG.Clear();
            lines_BG.Clear();
            connections_BG.Clear();

            layoutAlgorithm();

            foreach (var no in systemGraph.nodes) {
                var rect = new RectangleViewModel();

                rect.NodeName = no.name;
                List<string> localLabels = no.localLabels.ToList();

                List<string> localLabels_Copy = no.localLabels.ToList();

                foreach (var uu in localLabels_Copy) {
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
            foreach (var no in systemGraph.arcs) {
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

                foreach (var rects in rectangles_BG) {
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
            foreach (RectangleViewModel r in rectangles_BG) {
                BondGraphElement bgElement = new BondGraphElement(Graph_BG1, r.NodeName, r.Content, new Point(r.X, r.Y));
                rectElements.Add(r, bgElement);
            }

            // Create all BondArcs
            foreach (ConnectionViewModel c in connections_BG) {
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

        private void bondgraphSimplified() {
            options = (SimplificationRuleset.recognize(systemGraph, false, null));

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = SimplificationRuleset.recognize(systemGraph, false, null);
            }

            options = (directionRuleSet.recognize(systemGraph, false, null));

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = directionRuleSet.recognize(systemGraph, false, null);
            }

            options = (directionRuleSet2.recognize(systemGraph, false, null));

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = directionRuleSet2.recognize(systemGraph, false, null);
            }

            options = (directionRuleSet3.recognize(systemGraph, false, null));

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = directionRuleSet3.recognize(systemGraph, false, null);
            }

            options = (SimplificationRuleset2.recognize(systemGraph, false, null));

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = SimplificationRuleset2.recognize(systemGraph, false, null);
            }

            //again do a deepcopy of the systemGraph 

            //  designGraph SimplifiedGraphWithDir = systemGraph.copy(true);

            rectangles_BG_Simplified.Clear();
            lines_BG_Simplified.Clear();
            connections_BG_Simplified.Clear();

            foreach (var no in systemGraph.nodes) {
                var rect = new RectangleViewModel();

                rect.NodeName = no.name;
                List<string> localLabels = no.localLabels.ToList();

                List<string> localLabels_Copy = no.localLabels.ToList();

                foreach (var uu in localLabels_Copy) {
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
            foreach (var no in systemGraph.arcs) {
                if (no.localLabels.Contains("dir")) {
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

                    foreach (var rects in rectangles_BG_Simplified) {
                        if (rects.NodeName == no.From.name)
                            connect.Rect1 = rects;
                        if (rects.NodeName == no.To.name)
                            connect.Rect2 = rects;

                    }

                    connect.ConnectionMultiple = 1;
                    connect.ConnectionSide = "dir";
                    connections_BG_Simplified.Add(connect);
                } else {
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

                    foreach (var rects in rectangles_BG_Simplified) {
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
            foreach (RectangleViewModel r in rectangles_BG_Simplified) {
                BondGraphElement bgElement = new BondGraphElement(Graph_BG2, r.NodeName, r.Content, new Point(r.X, r.Y));
                rectElements.Add(r, bgElement);
            }

            // Create all BondArcs
            foreach (ConnectionViewModel c in connections_BG_Simplified) {
                if (!c.ConnectionSide.Contains("nondir")) {
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

        private void checkIfVelocityDirectionsAreOkay(out bool noGood) {
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
            ruleSet VerifyBGDir = (ruleSet) ruleDeserializer.Deserialize(ruleReader);
            //VerifyBGDir.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";
            int numLoaded;
            var numRules = VerifyBGDir.ruleFileNames.Count;
            VerifyBGDir.rules = LoadRulesFromFileNames(VerifyBGDir.rulesDir, VerifyBGDir.ruleFileNames, out numLoaded);
            ruleReader.Dispose();

            foreach (var item in VerifyBGDir.rules) {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            //second step is to verify if all nodes have velocity directions 

            options = (VerifyBGDir.recognize(systemGraph, false, null));

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = SimplificationRuleset.recognize(systemGraph, false, null);
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

    }

}

