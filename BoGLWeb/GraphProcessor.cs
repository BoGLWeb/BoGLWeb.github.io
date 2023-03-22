using BoGLWeb.BaseClasses;
using BoGLWeb.Prop;
using GraphSynth.Representation;

namespace BoGLWeb {
    public class BondGraphFactory {

        /// <summary>
        /// Creates a set of bond graphs from a designGraph
        /// </summary>
        /// <param name="systemGraph">The design graph to process</param>
        /// <returns>First element of tuple is unsimplifiedBG, second is simplifiedBG, last is list of causalBGs</returns>
        /// <exception cref="Exception">Thrown if the any generated BondGraphs are null</exception>
        public static (BondGraph, BondGraph, List<BondGraph>) generateBondGraphs(designGraph systemGraph) {
            BondGraphFactory factory = new(systemGraph);

            //Check that we create all three bond graphs
            switch (factory.simplifiedBG) {
                case null when factory.unsimplifiedBG is null:
                    //TODO Need to figure out what this error means and figure out how to show it to the user
                    throw new Exception("simplifiedBG and unsimplifiedBG are null");
                case null:
                    //TODO Need to figure out what this error means and figure out how to show it to the user
                    throw new Exception("simplifiedBG is null");
            }

            if (factory.unsimplifiedBG is null) {
                //TODO Need to figure out what this error means and figure out how to show it to the user
                throw new Exception("unsimplifiedBG is null");
            }

            //Convert graph synth bond graphs to our bond graph class
            BondGraph unsimplified = BondGraph.generateBondGraphFromGraphSynth(factory.unsimplifiedBG);
            BondGraph simplified = BondGraph.generateBondGraphFromGraphSynth(factory.simplifiedBG);

            List<BondGraph> causalBGs = factory.finalresult.Select(BondGraph.generateBondGraphFromGraphSynth).ToList();

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

        //Create a bond graph factory
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

            this.generateBondGraph();
        }

        //Mainly code from desktop BoGL below
        //TODO cleanup variable names and old comments
        private void generateBondGraph() {
            //now remove all the labels that we added
            //need to return bool value if vel directions are fine or not. 
            //assigning I: and C: nodes with some identifier

            // bool noGood = true;
            checkIfVelocityDirectionsAreOkay(out bool noGood);

            if (!noGood) {
                //TODO Find a better way to display this error message
                const string message = "There was an issue with the velocity directions in your System Diagram. Please correct them and select generate again.";
                Console.WriteLine(message);
                throw new ArgumentException(message);
            } else {
                this.bondgraphBeforeSimplification();
                this.bondgraphSimplified();
                this.obtainCausality();
            }
        }

        private void obtainCausality() {
            this.causalBGs.Clear();
            this.nodeLabelSorted.Clear();
            this.sortedIndices.Clear();

            this.finalresult.Clear();
            this.optiGraphs.Clear();

            //will just do one option for now, will figure out 
            #region initial causality
            Stack<designGraph> sysGraphs = new();

            sysGraphs.Push(systemGraph.copy());
            designGraph sys = sysGraphs.Pop();
            this.options = RuleSetMap.getInstance().getRuleSet("NewCausalityMethodRuleset").recognize(sys, false, null);
            while (this.options.Count > 0) {
                this.options[0].apply(sys, null);
                this.options = RuleSetMap.getInstance().getRuleSet("NewCausalityMethodRuleset").recognize(sys, false, null);
            }

            sysGraphs.Push(sys);
            while (sysGraphs.Count > 0) {
                sys = sysGraphs.Pop();
                this.options = RuleSetMap.getInstance().getRuleSet("NewCausalityMethodRuleset_2").recognize(sys, false, null);
                if (this.options.Count > 0) {
                    foreach (option opt in this.options) {
                        designGraph gra = sys.copy();
                        GraphSynth.Search.SearchProcess.transferLmappingToChild(gra, sys, opt.nodes, opt.arcs, opt.hyperarcs);
                        opt.apply(gra, null);
                        sysGraphs.Push(gra);
                    }
                } else
                    this.sys_Graphs.Add(sys);
            }

            foreach (designGraph item in this.sys_Graphs) {
                sys = item.copy();
                this.options = RuleSetMap.getInstance().getRuleSet("NewCausalityMethodRuleset_3").recognize(sys, false, null);

                while (this.options.Count > 0) {
                    this.options[0].apply(sys, null);
                    this.options = RuleSetMap.getInstance().getRuleSet("NewCausalityMethodRuleset_3").recognize(sys, false, null);
                }
                sysGraphs.Push(sys);

            }

            this.sys_Graphs.Clear();

            #endregion 


            while (sysGraphs.Count > 0) {
                sys = sysGraphs.Pop();
                this.options = RuleSetMap.getInstance().getRuleSet("INVDMarkerRules").recognize(sys, false, null);
                while (this.options.Count > 0) {
                    this.options[0].apply(sys, null);
                    this.options = RuleSetMap.getInstance().getRuleSet("INVDMarkerRules").recognize(sys, false, null);
                }

                Stack<designGraph> graphss = new();
                List<designGraph> graph_SSS = new();
                graphss.Push(sys);

                {
                    designGraph graphS = graphss.Pop();
                    List<option> options1 = RuleSetMap.getInstance().getRuleSet("CalibrationNewRuleset").recognize(graphS, false, null);

                    if (options1.Count > 0) {
                        foreach (option opt in options1) {
                            designGraph graphSS = graphS.copy();
                            GraphSynth.Search.SearchProcess.transferLmappingToChild(graphSS, graphS, opt.nodes,
                                                               opt.arcs, opt.hyperarcs);
                            opt.apply(graphSS, null);
                            graph_SSS.Add(graphSS);
                        }
                    } else
                        graph_SSS.Add(graphS);

                }

                foreach (designGraph opt in graph_SSS) {
                    List<option> options1 = RuleSetMap.getInstance().getRuleSet("CalibrationNewRuleset").recognize(opt, false, null);

                    while (options1.Count > 0) {
                        options1[0].apply(opt, null);
                        options1 = RuleSetMap.getInstance().getRuleSet("CalibrationNewRuleset").recognize(opt, false, null);
                    }

                    graphss.Push(opt);
                }

                graph_SSS.Clear();

                while (graphss.Count > 0) {
                    designGraph graphS = graphss.Pop();
                    List<option> options1 = RuleSetMap.getInstance().getRuleSet("RFlagCleanRuleset").recognize(graphS, false, null);

                    while (options1.Count > 0) {
                        options1[0].apply(graphS, null);
                        options1 = RuleSetMap.getInstance().getRuleSet("RFlagCleanRuleset").recognize(graphS, false, null);
                    }
                    graph_SSS.Add(graphS);
                }
                foreach (designGraph opt in graph_SSS) {
                    graphss.Push(opt);
                }

                graph_SSS.Clear();

                while (graphss.Count > 0) {
                    designGraph graphS = graphss.Pop();
                    List<option> options1 = RuleSetMap.getInstance().getRuleSet("ICFixTotalRuleset").recognize(graphS, false, null);

                    while (options1.Count > 0) {
                        options1[0].apply(graphS, null);
                        options1 = RuleSetMap.getInstance().getRuleSet("ICFixTotalRuleset").recognize(graphS, false, null);
                    }

                    graph_SSS.Add(graphS);
                }

                foreach (designGraph op in graph_SSS) {
                    this.options = RuleSetMap.getInstance().getRuleSet("TransformerFlipRuleset").recognize(op, false, null);

                    if (this.options.Count > 0) {
                        this.options[0].apply(op, null);
                        sysGraphs.Push(op);
                    } else {
                        this.options = RuleSetMap.getInstance().getRuleSet("TransformerFlipRuleset2").recognize(op, false, null);

                        if (this.options.Count > 0) {
                            this.options[0].apply(op, null);
                            sysGraphs.Push(op);
                        } else
                            this.optiGraphs.Add(op);
                    }


                }
            }

            foreach (designGraph opt in this.optiGraphs) {
                this.options = RuleSetMap.getInstance().getRuleSet("Clean23Ruleset").recognize(opt, false, null);

                while (this.options.Count > 0) {
                    this.options[0].apply(opt, null);
                    this.options = RuleSetMap.getInstance().getRuleSet("Clean23Ruleset").recognize(opt, false, null);
                }

                this.finalresult.Add(opt);
            }

            this.indiceswithoutINVD.Clear();
            this.maxIntegralCausality.Clear();
            for (int ii = 0; ii < this.finalresult.Count; ii++) {
                bool index = checkINVD(this.finalresult[ii]);

                if (index) {
                    continue;
                }

                this.indiceswithoutINVD.Add(ii);
                this.maxIntegralCausality.Add(checkICs(this.finalresult[ii]));
            }

            //now from the list of finalgraph, eliminate duplicate solutions
            if (this.indiceswithoutINVD.Count == 0) {
                const string message = "An error has occurred with respect to Causality assignment. Please check that your System Diagram is correct and then select generate again.";
                Console.WriteLine(message);
                throw new ArgumentException(message);
            }
            //need to add exception here if the program is unable to added 
            else {
                int currentHashSetcount = 0;
                int nn = 0;
                foreach (int n in this.indiceswithoutINVD) {
                    List<string> nodeLabels_Cau = new();
                    designGraph cauGraph = this.finalresult[n];

                    foreach (arc arcC in cauGraph.arcs) {
                        if (arcC.localLabels.Contains("I2") && arcC.localLabels.Contains("SAME")) {
                            nodeLabels_Cau.AddRange(arcC.To.localLabels.Where(t => t.Contains("iadded")));
                        }

                        if (!arcC.localLabels.Contains("C3") || !arcC.localLabels.Contains("OPP")) {
                            continue;
                        }

                        for (int iii = 0; iii < arcC.From.localLabels.Count; iii++) {
                            if (arcC.From.localLabels[iii].Contains("cadded")) {
                                nodeLabels_Cau.Add(arcC.From.localLabels[iii]);

                            }

                        }

                    }

                    if (nodeLabels_Cau.Count > 0) {
                        nodeLabels_Cau.Sort();

                        string combined = nodeLabels_Cau.Aggregate("", (current, x) => current + x);

                        this.nodeLabelSorted.Add(combined);

                        //need to add index numbers as well into a list
                        if ((nn + 1) == this.nodeLabelSorted.Count) //for the first one, it will be same
                        {
                            this.sortedIndices.Add(n);
                            currentHashSetcount++;
                        }

                        if (currentHashSetcount <= this.nodeLabelSorted.Count) {
                            this.sortedIndices.Add(n);
                            currentHashSetcount++;
                        }

                    }
                    nn++;
                }

                List<int> maxIntegralCaus = this.sortedIndices.Select(n => this.indiceswithoutINVD.FindIndex(item => item == n)).Select(indexindex => this.maxIntegralCausality[indexindex]).ToList();

                //  foreach (var no in finalresult[index1].nodes)

                //now add to the combo-box

                this.causalBGs.Clear();
            }
        }

        private void bondgraphBeforeSimplification() {
            this.options = RuleSetMap.getInstance().getRuleSet("BondGraphRuleset").recognize(this.systemGraph, true, null);

            while (this.options.Count > 0) {
                this.options[0].apply(this.systemGraph, null);
                this.options = RuleSetMap.getInstance().getRuleSet("BondGraphRuleset").recognize(this.systemGraph, true, null);

            }

            List<string> nodeLabels = new();
            List<int> list = new();
            int ll = 0;
            foreach (node no in this.systemGraph.nodes) {
                nodeLabels.Add(string.Join(string.Empty, no.localLabels.ToArray()));
                list.Add(ll++);
            }
            ll = 0;
            foreach (node opt in this.systemGraph.nodes) {
                opt.name = "name" + list[ll];
                ll++;
            }

            //try to update the positions of each node

            this.unsimplifiedBG = this.systemGraph.copy(true);
        }

        private void bondgraphSimplified() {
            this.options = RuleSetMap.getInstance().getRuleSet("SimplificationRuleset").recognize(this.systemGraph, false, null);

            while (this.options.Count > 0) {
                this.options[0].apply(this.systemGraph, null);
                this.options = RuleSetMap.getInstance().getRuleSet("SimplificationRuleset").recognize(this.systemGraph, false, null);
            }

            this.options = RuleSetMap.getInstance().getRuleSet("DirRuleset").recognize(this.systemGraph, false, null);

            while (this.options.Count > 0) {
                this.options[0].apply(this.systemGraph, null);
                this.options = RuleSetMap.getInstance().getRuleSet("DirRuleset").recognize(this.systemGraph, false, null);
            }

            this.options = RuleSetMap.getInstance().getRuleSet("newDirectionRuleSet_2").recognize(this.systemGraph, false, null);

            while (this.options.Count > 0) {
                this.options[0].apply(this.systemGraph, null);
                this.options = RuleSetMap.getInstance().getRuleSet("newDirectionRuleSet_2").recognize(this.systemGraph, false, null);
            }

            this.options = RuleSetMap.getInstance().getRuleSet("DirRuleset3").recognize(this.systemGraph, false, null);

            while (this.options.Count > 0) {
                this.options[0].apply(this.systemGraph, null);
                this.options = RuleSetMap.getInstance().getRuleSet("DirRuleset3").recognize(this.systemGraph, false, null);
            }

            this.options = RuleSetMap.getInstance().getRuleSet("Simplification2").recognize(this.systemGraph, false, null);

            while (this.options.Count > 0) {
                this.options[0].apply(this.systemGraph, null);
                this.options = RuleSetMap.getInstance().getRuleSet("Simplification2").recognize(this.systemGraph, false, null);
            }

            //again do a deepcopy of the systemGraph 

            //  designGraph SimplifiedGraphWithDir = systemGraph.copy(true);

            this.simplifiedBG = this.systemGraph.copy();

            //now let us apply directions - first let us do the sources and i-c-r 

            //all sources should have arrow heads away from source 
            //all I-C-R should have arrow heads into them. 
        }

        private void checkIfVelocityDirectionsAreOkay(out bool noGood) {
            //works for mechanical translation systems only - no mechanical rotation yet

            //first load the verify direction rules 

            foreach (grammarRule item in RuleSetMap.getInstance().getRuleSet("BeforeBG-VerifyDirRuleSet").rules) {
                item.TransformNodePositions = false;
                item.Rotate = false;
            }

            //second step is to verify if all nodes have velocity directions 

            this.options = RuleSetMap.getInstance().getRuleSet("BeforeBG-VerifyDirRuleSet").recognize(this.systemGraph, false, null);

            while (this.options.Count > 0) {
                this.options[0].apply(this.systemGraph, null);
                this.options = RuleSetMap.getInstance().getRuleSet("SimplificationRuleset").recognize(this.systemGraph, false, null);
            }

            //now check if all the nodes that have veladded label have good label as well

            noGood = this.systemGraph.nodes.All(n => !n.localLabels.Contains("veladded") || n.localLabels.Contains("good"));
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
            foreach (string x in designGraph.nodes.SelectMany(n => n.localLabels)) {
                if (x.Contains("INVD"))
                    return true;
                if (x.Contains("Flipped"))
                    return true;
            }

            return false;
        }

    }

}

