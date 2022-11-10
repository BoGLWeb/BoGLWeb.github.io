using AVL_Prototype_1;
using GraphSynth.Representation;
using System.Runtime.CompilerServices;

namespace BoGLWeb {
    public class BondGraphFactory{

        //Returns a list. First element is the Unsimplified BG,
        //Second is the Simplified BG and rest are the Causal BGs
        public static List<BondGraph> generateBondGraphs(designGraph systemGraph) {
            BondGraphFactory factory = new BondGraphFactory(systemGraph);
            
            return null;
        }

        private List<option> options;
        private List<designGraph> sys_Graphs;
        private HashSet<string> nodeLabelSorted;
        private HashSet<int> sortedIndices;
        private List<designGraph> optiGraphs;
        private List<designGraph> finalresult;
        private List<int> indiceswithoutINVD;
        private List<int> maxIntegralCausality;
        private int index1;
        private bool causality;
        private designGraph systemGraph;

        private BondGraphFactory(designGraph systemGraph) {
            options = new List<option>();
            sys_Graphs = new List<designGraph>();
            nodeLabelSorted = new HashSet<string>();
            finalresult = new List<designGraph>();
            indiceswithoutINVD = new List<int>();
            maxIntegralCausality = new List<int>();
            index1 = 0;
            causality = false;
            this.systemGraph = systemGraph;
            generateBondGraph();
        }

        public void generateBondGraph() {
            checkIfVelocityDirectionsAreOkay(systemGraph, out bool noGood);

            if (!noGood) {
                Console.WriteLine("There was an issue");
            } else {
                bondgraphBeforeSimplification();
                bondgraphSimplified();

                int ii = 0;
                foreach (var n in systemGraph.nodes) {
                    if (n.localLabels.Contains("I:")) {
                        n.localLabels.Add("iadded" + (ii.ToString()));
                    }
                    if (n.localLabels.Contains("C:")) {
                        n.localLabels.Add("cadded" + (ii.ToString()));
                    }
                    ii++;
                }
                obtainCausality();
            }
        }

        private int checkICs(designGraph designGraph) {
            int xx = 0;
            foreach (arc a in designGraph.arcs) {
                if (a.localLabels.Contains("I2") && a.localLabels.Contains("SAME"))
                    xx = xx + 1;
                if (a.localLabels.Contains("C3") && a.localLabels.Contains("OPP"))
                    xx = xx + 1;
            }

            return (xx);

        }

        private bool checkINVD(designGraph designGraph) {
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

        private void obtainCausality() {
            nodeLabelSorted.Clear();
            sortedIndices.Clear();
            finalresult.Clear();
            optiGraphs.Clear();

            //will just do one option for now, will figure out
            #region initial causality
            Stack<designGraph> sysGraphs = new Stack<designGraph>();

            sysGraphs.Push(systemGraph.copy());
            var sys = sysGraphs.Pop();
            options = (RuleSetMap.getInstance().getRuleSet("NewCausalityMethodRuleset").recognize(sys, false, null));

            while (options.Count > 0) {
                options[0].apply(sys, null);
                options = RuleSetMap.getInstance().getRuleSet("NewCausalityMethodRuleset").recognize(sys, false, null);
            }

            sysGraphs.Push(sys);
            while (sysGraphs.Count > 0) {
                sys = sysGraphs.Pop();
                options = (RuleSetMap.getInstance().getRuleSet("NewCausalityMethodRuleset_2").recognize(sys, false, null));
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
                options = (RuleSetMap.getInstance().getRuleSet("NewCausalityMethodRuleset_3").recognize(sys, false, null));

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
                options = (RuleSetMap.getInstance().getRuleSet("INVDMarkerRules").recognize(sys, false, null));
                while (options.Count > 0) {
                    options[0].apply(sys, null);
                    options = RuleSetMap.getInstance().getRuleSet("INVDMarkerRules").recognize(sys, false, null);
                }

                Stack<designGraph> graphss = new Stack<designGraph>();
                List<designGraph> graph_SSS = new List<designGraph>();
                graphss.Push(sys);

                {
                    var graphS = graphss.Pop();
                    var options1 = (RuleSetMap.getInstance().getRuleSet("CalibrationNewRuleset").recognize(graphS, false, null));

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
                    var options1 = (RuleSetMap.getInstance().getRuleSet("CalibrationNewRuleset").recognize(opt, false, null));

                    while (options1.Count > 0) {
                        options1[0].apply(opt, null);
                        options1 = RuleSetMap.getInstance().getRuleSet("CalibrationNewRuleset").recognize(opt, false, null);
                    }

                    graphss.Push(opt);
                }

                graph_SSS.Clear();

                while (graphss.Count > 0) {
                    var graphS = graphss.Pop();
                    var options1 = (RuleSetMap.getInstance().getRuleSet("RFlagCleanRuleset").recognize(graphS, false, null));

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
                        options1 = (RuleSetMap.getInstance().getRuleSet("RFlagCleanRuleset").recognize(graphS, false, null));
                    }
                    graph_SSS.Add(graphS);
                }
                foreach (var opt in graph_SSS) {
                    graphss.Push(opt);
                }

                graph_SSS.Clear();

                while (graphss.Count > 0) {
                    var graphS = graphss.Pop();
                    var options1 = (RuleSetMap.getInstance().getRuleSet("ICFixTotalRuleset").recognize(graphS, false, null));

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
                        options1 = (RuleSetMap.getInstance().getRuleSet("ICFixTotalRuleset").recognize(graphS, false, null));
                    }

                    graph_SSS.Add(graphS);

                }

                foreach (var op in graph_SSS) {
                    options = (RuleSetMap.getInstance().getRuleSet("TransformerFlipRuleset").recognize(op, false, null));

                    if (options.Count > 0) {
                        options[0].apply(op, null);

                        sysGraphs.Push(op);

                    } else {


                        options = (RuleSetMap.getInstance().getRuleSet("TransformerFlipRuleset2").recognize(op, false, null));

                        if (options.Count > 0) {

                            options[0].apply(op, null);
                            sysGraphs.Push(op);
                        } else
                            optiGraphs.Add(op);
                    }


                }
            }

            foreach (var opt in optiGraphs) {
                options = (RuleSetMap.getInstance().getRuleSet("Clean23Ruleset").recognize(opt, false, null));

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

                    Console.WriteLine(stringtobeadded);
                }

                causality = true;
            }
        }

        private void bondgraphSimplified() {
            options = (RuleSetMap.getInstance().getRuleSet("SimplificationRuleset").recognize(systemGraph, false, null));

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = RuleSetMap.getInstance().getRuleSet("SimplificationRuleset").recognize(systemGraph, false, null);
            }

            options = (RuleSetMap.getInstance().getRuleSet("DirRuleset").recognize(systemGraph, false, null));

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = RuleSetMap.getInstance().getRuleSet("DirRuleset").recognize(systemGraph, false, null);
            }

            options = (RuleSetMap.getInstance().getRuleSet("newDirectionRuleSet_2").recognize(systemGraph, false, null));

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = RuleSetMap.getInstance().getRuleSet("newDirectionRuleSet_2").recognize(systemGraph, false, null);
            }

            options = (RuleSetMap.getInstance().getRuleSet("DirRuleset3").recognize(systemGraph, false, null));

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = RuleSetMap.getInstance().getRuleSet("DirRuleset3").recognize(systemGraph, false, null);
            }

            options = (RuleSetMap.getInstance().getRuleSet("Simplification2").recognize(systemGraph, false, null));

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = RuleSetMap.getInstance().getRuleSet("Simplification2").recognize(systemGraph, false, null);
            }

            //again do a deepcopy of the systemGraph

            //  designGraph SimplifiedGraphWithDir = systemGraph.copy(true);

            foreach (var no in systemGraph.nodes) {
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
            }

            //now let us apply directions - first let us do the sources and i-c-r
            //all sources should have arrow heads away from source
            //all I-C-R should have arrow heads into them.
        }


        private void bondgraphBeforeSimplification() {
            options = (RuleSetMap.getInstance().getRuleSet("BondGraphRuleset").recognize(systemGraph, true, null));

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = RuleSetMap.getInstance().getRuleSet("BondGraphRuleset").recognize(systemGraph, true, null);

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

            foreach (var no in systemGraph.nodes) {
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
            }
        }

        private void checkIfVelocityDirectionsAreOkay(designGraph systemGraph, out bool noGood) {
            //works for mechanical translation systems only - no mechanical rotation yet

            //first load the verify direction rules

            /*var filename = "Rules\\BeforeBG-VerifyDirRuleSet.rsxml";
            //  var filename = extractPath1 + "\\BondGraphRuleset.rsxml";
            ruleReader = new StreamReader(filename);*/


            /*   var assembly = Assembly.GetExecutingAssembly();
            var filename = "AVL_Prototype_1.Rules.BeforeBG-VerifyDirRuleSet.rsxml";
            var stream = assembly.GetManifestResourceStream(filename);
               ruleReader = new StreamReader(stream); */

            //Uri uri = new Uri("/Rules/BeforeBG-VerifyDirRuleSet.rsxml", UriKind.Relative);
            //System.Windows.Resources.StreamResourceInfo info = Application.GetResourceStream(uri);
            //ruleReader = new StreamReader(info.Stream);

            //var ruleDeserializer = new XmlSerializer(typeof(ruleSet));
            //ruleSet VerifyBGDir = (ruleSet)ruleDeserializer.Deserialize(ruleReader);
            ////VerifyBGDir.rulesDir = System.IO.Path.GetDirectoryName(filename) + @"\";
            //int numLoaded;
            //var numRules = VerifyBGDir.ruleFileNames.Count;
            //VerifyBGDir.rules = LoadRulesFromFileNames(VerifyBGDir.rulesDir, VerifyBGDir.ruleFileNames, out numLoaded);
            //ruleReader.Dispose();

            foreach (var item in RuleSetMap.getInstance().getRuleSet("BeforeBG-VerifyDirRuleSet").rules) {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            //second step is to verify if all nodes have velocity directions
            options = (RuleSetMap.getInstance().getRuleSet("BeforeBG-VerifyDirRuleSet").recognize(systemGraph, false, null));

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

    }
}
