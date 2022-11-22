using BoGLWeb.BaseClasses;
using BoGLWeb.Prop;

namespace BoGLWeb {
    public class BondGraphFactory {

        /// <summary>
        /// Creates a set of bondgraphs from a designGraph
        /// </summary>
        /// <param name="systemGraph">The design graph to process</param>
        /// <returns>First element of tuple is unsimplifiedBG, second is simplifiedBG, last is list of causalBGs</returns>
        /// <exception cref="Exception">Thrown if the any generated BondGraphs are null</exception>
        public static (BondGraph, BondGraph, List<BondGraph>) generateBondGraphs(designGraph systemGraph) {
            Console.WriteLine("System Graph");
            Console.WriteLine(systemGraph.ToString());
            BondGraphFactory factory = new(systemGraph);

            if (factory.simplifiedBG is null && factory.unsimplifiedBG is null) {
                //TODO Need to figure out what this error means and figure out how to show it to the user
                throw new Exception("simplifiedBG and unsimplifiedBG are null");
            }

            if (factory.simplifiedBG is null) {
                //TODO Need to figure out what this error means and figure out how to show it to the user
                throw new Exception("simplifiedBG is null");
            }

            if (factory.unsimplifiedBG is null) {
                //TODO Need to figure out what this error means and figure out how to show it to the user
                throw new Exception("unsimplifiedBG is null");
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

                List<int> maxIntegralCaus = new();

                foreach (var n in sortedIndices) {
                    //use the indiceswithoutINVD to obtain the index in that list

                    var indexindex = indiceswithoutINVD.FindIndex(item => item == n);
                    maxIntegralCaus.Add(maxIntegralCausality[indexindex]);

                }

                //  foreach (var no in finalresult[index1].nodes)

                //now add to the combo-box

                causalBGs.Clear();
            }
        }

        private void bondgraphBeforeSimplification() {
            options = RuleSetMap.getInstance().getRuleSet("BondGraphRuleset").recognize(systemGraph, true, null);

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = RuleSetMap.getInstance().getRuleSet("BondGraphRuleset").recognize(systemGraph, true, null);

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

            unsimplifiedBG = systemGraph.copy(true);
        }

        private void bondgraphSimplified() {
            options = RuleSetMap.getInstance().getRuleSet("SimplificationRuleset").recognize(systemGraph, false, null);

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = RuleSetMap.getInstance().getRuleSet("SimplificationRuleset").recognize(systemGraph, false, null);
            }

            options = RuleSetMap.getInstance().getRuleSet("DirRuleset").recognize(systemGraph, false, null);

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = RuleSetMap.getInstance().getRuleSet("DirRuleset").recognize(systemGraph, false, null);
            }

            options = RuleSetMap.getInstance().getRuleSet("newDirectionRuleSet_2").recognize(systemGraph, false, null);

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = RuleSetMap.getInstance().getRuleSet("newDirectionRuleSet_2").recognize(systemGraph, false, null);
            }

            options = RuleSetMap.getInstance().getRuleSet("DirRuleset3").recognize(systemGraph, false, null);

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = RuleSetMap.getInstance().getRuleSet("DirRuleset3").recognize(systemGraph, false, null);
            }

            options = RuleSetMap.getInstance().getRuleSet("Simplification2").recognize(systemGraph, false, null);

            while (options.Count > 0) {
                options[0].apply(systemGraph, null);
                options = RuleSetMap.getInstance().getRuleSet("Simplification2").recognize(systemGraph, false, null);
            }

            //again do a deepcopy of the systemGraph 

            //  designGraph SimplifiedGraphWithDir = systemGraph.copy(true);

            simplifiedBG = systemGraph.copy(true);

            //now let us apply directions - first let us do the sources and i-c-r 

            //all sources should have arrow heads away from source 
            //all I-C-R should have arrow heads into them. 
        }

        private void checkIfVelocityDirectionsAreOkay(out bool noGood) {
            //works for mechanical translation systems only - no mechanical rotation yet

            //first load the verify direction rules 

            foreach (var item in RuleSetMap.getInstance().getRuleSet("BeforeBG-VerifyDirRuleSet").rules) {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            //second step is to verify if all nodes have velocity directions 

            options = RuleSetMap.getInstance().getRuleSet("BeforeBG-VerifyDirRuleSet").recognize(systemGraph, false, null);

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

