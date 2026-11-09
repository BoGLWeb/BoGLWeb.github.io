/*
===================================================================================================
BOND GRAPH GENERATION, TOPOLOGY SYNTHESIS & LAYOUT PIPELINE
===================================================================================================

                [ Physical System Sketch / Domain Input ]
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│ STEP 1: SPATIAL ORIENTATION & DOMAIN PARSER                                     │
│   • Bounding Box Analysis:                                                      │
│       - ΔX > ΔY        => Mechanical Horizontal (Left-to-Right power spine)     │
│       - ΔY >= ΔX       => Mechanical Vertical (Bottom-to-Top or Top-to-Bottom)   │
│       - Electrical     => Hybrid 2D Orthogonal (Planar circuit mesh)            │
│   • Boundary Anchor Synchronization:                                            │
│       - Sorts boundary elements (Ground vs. Source) by spatial coordinates      │
│       - Prevents spatial inversion (e.g., Ground on Left => 1GND on Left)       │
└─────────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│ STEP 2: UNREDUCED / UNSIMPLIFIED GRAPH SYNTHESIS                                │
│   • Common Velocity Junctions (1-junctions) mapped to inertia / masses          │
│   • Relative Velocity Junctions (0-junctions) mapped to force-transmitting pairs│
│   • Symmetric Diamond Parallel Routing:                                         │
│       - Two elements in parallel between two 1-junctions (e.g., K and b)        │
│         branch symmetrically via dual 0-junctions                               │
│       - Eliminates legacy asymmetric in-line trunk collapsing                   │
└─────────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│ STEP 3: SIMPLIFIED GRAPH COORDINATE SOLVER & COLLISION AVOIDANCE                │
│   • Reduction: Merge trivial 0/1 junctions and identity bonds                   │
│   • Primary Spine Placement: Lay out main power flow along principal axis       │
│   • Compact Proportions: Bond lengths shortened (~30%) for compact layout       │
│   • Orthogonal Leaf Port Distributor:                                           │
│       - Distribute 1-port leaves (I, R, C, Se, Sf) to available cardinal        │
│         directions (Up, Down, Left, Right) relative to parent junction          │
│       - Prohibit collinear leaf stacking along identical rays                   │
│       - Eliminates through-bond intersections traversing element labels         │
└─────────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│ STEP 4: CAUSAL BOND GRAPH DERIVATION                                            │
│   • Causality Assignment: Apply Sequential Causality Assignment Procedure (SCAP)│
│   • Coordinate Inheritance: DIRECTLY inherit (X, Y) node coordinates from       │
│     Simplified Graph (prevents unseeded force-directed starburst/drift)         │
│   • Bond Decoration: Overlay causal strokes and half-arrows orthogonally        │
│   • Annotation Clearances: Apply bounding-box margins for effort/flow labels    │
│     (F_i, v_i, p', x', tau_i, omega_i) to prevent text collisions               │
└─────────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│ STEP 5: SVG / CANVAS RENDERING & TYPOGRAPHIC NORMALIZATION                      │
│   • Decouple font sizing from SVG viewBox bounding-box auto-scaling             │
│   • Enforce uniform typography hierarchy (13px elements, 11px annotations)     │
│   • Ensure consistent line weights and non-scaling bond arrows                  │
└─────────────────────────────────────────────────────────────────────────────────┘
===================================================================================================
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace BoGLWeb {
    public enum LayoutOrientation {
        Horizontal,
        Vertical,
        ElectricalHybrid
    }

    /// <summary>
    /// Core layout engine and coordinate solver for bond graphs.
    /// Provides orthogonal grid embedding, principal-axis spine routing, symmetric parallel diamond branching,
    /// dedicated collision-free leaf port allocation, and direct causal coordinate inheritance.
    /// </summary>
    public class BondGraphEmbedder {
        public const double GridSpacingX = 112.0;
        public const double GridSpacingY = 112.0;

        private bool optimized;
        private readonly BondGraph bondGraph;
        private readonly LayoutOrientation orientation;
        private readonly bool flowLeftToRight;
        private readonly bool flowBottomToTop;

        /// <summary>
        /// Take in a bond graph that will be embedded into a canvas with an orthogonal layout.
        /// </summary>
        /// <param name="bondGraph">A bond graph</param>
        /// <param name="orientation">Desired layout orientation (Horizontal, Vertical, ElectricalHybrid)</param>
        /// <param name="flowLeftToRight">Whether the primary power flow moves left-to-right (Ground on left, Force on right)</param>
        /// <param name="flowBottomToTop">Whether the primary power flow moves bottom-to-top (Source at bottom, load at top)</param>
        public BondGraphEmbedder(BondGraph bondGraph, LayoutOrientation orientation = LayoutOrientation.Horizontal, bool flowLeftToRight = true, bool flowBottomToTop = false) {
            this.optimized = false;
            this.bondGraph = bondGraph;
            this.orientation = orientation;
            this.flowLeftToRight = flowLeftToRight;
            this.flowBottomToTop = flowBottomToTop;
        }

        /// <summary>
        /// Orientation Detection: Determines the primary power spine layout axis from physical sketch geometry.
        /// - Mechanical Systems: Computes the sketch bounding box. If aspect ratio DeltaX > DeltaY, horizontal
        ///   orientation (left-to-right) is selected. If DeltaY >= DeltaX, vertical orientation is selected.
        /// - Electrical Systems: Identified by electrical components (resistors, inductors, capacitors, sources).
        ///   These use a 2D hybrid orthogonal layout suited for planar circuit loops.
        /// </summary>
        public static LayoutOrientation ResolveOrientation(SystemDiagram? diagram) {
            if (diagram == null) return LayoutOrientation.Horizontal;
            return ResolveOrientation(diagram.getElements());
        }

        /// <summary>
        /// Resolves orientation from a collection of SystemDiagram elements based on domain and aspect ratio.
        /// </summary>
        public static LayoutOrientation ResolveOrientation(IEnumerable<SystemDiagram.Element>? elements) {
            if (elements == null || !elements.Any()) return LayoutOrientation.Horizontal;

            // Check domain: if any element is electrical (types 19 to 26, or name contains "System_E_")
            bool isElectrical = elements.Any(e => {
                int t = e.getType();
                return (t >= 19 && t <= 26) || (!string.IsNullOrEmpty(e.getName()) && e.getName().StartsWith("System_E_"));
            });

            if (isElectrical) {
                return LayoutOrientation.ElectricalHybrid;
            }

            // Mechanical translational / rotational: compute bounding box
            double minX = elements.Min(e => e.getX());
            double maxX = elements.Max(e => e.getX());
            double minY = elements.Min(e => e.getY());
            double maxY = elements.Max(e => e.getY());

            double deltaX = maxX - minX;
            double deltaY = maxY - minY;

            return deltaX > deltaY ? LayoutOrientation.Horizontal : LayoutOrientation.Vertical;
        }

        /// <summary>
        /// Anchor Alignment (Left-to-Right): Synchronizes horizontal bond graph directionality with the physical sketch.
        /// Compares the spatial X-coordinates of boundary anchors:
        /// - Ground constraints (1GND) vs. input sources (Se:Force, Sf:Velocity).
        /// - When Ground is physically located to the left of Source (X_ground <= X_source), returns true,
        ///   ensuring 1GND is anchored on the left and Se:Force is placed on the right, matching user intuition.
        /// </summary>
        public static bool ResolveFlowLeftToRight(SystemDiagram? diagram) {
            if (diagram == null) return true;
            return ResolveFlowLeftToRight(diagram.getElements());
        }

        /// <summary>
        /// Resolves whether a collection of SystemDiagram elements flows Left-to-Right along the X-axis.
        /// </summary>
        public static bool ResolveFlowLeftToRight(IEnumerable<SystemDiagram.Element>? elements) {
            if (elements == null || !elements.Any()) return true;

            var elementList = elements.ToList();
            var groundElements = elementList.Where(e => {
                int t = e.getType();
                return t == 3 || t == 24 || (!string.IsNullOrEmpty(e.getName()) && e.getName().Contains("Ground"));
            }).ToList();

            var sourceElements = elementList.Where(e => {
                int t = e.getType();
                return t == 4 || t == 6 || t == 10 || t == 11 || t == 25 || t == 26 ||
                       (!string.IsNullOrEmpty(e.getName()) && (e.getName().Contains("Force") || e.getName().Contains("Input")));
            }).ToList();

            if (groundElements.Count > 0 && sourceElements.Count > 0) {
                double avgGroundX = groundElements.Average(e => e.getX());
                double avgSourceX = sourceElements.Average(e => e.getX());
                return avgGroundX <= avgSourceX;
            }

            // Fallback: check order of first and last elements
            double firstX = elementList.First().getX();
            double lastX = elementList.Last().getX();
            return firstX <= lastX;
        }

        /// <summary>
        /// Anchor Alignment (Bottom-to-Top): Synchronizes vertical bond graph directionality with the physical sketch.
        /// Compares spatial Y-coordinates in canvas space (where larger Y is physically lower on the canvas):
        /// - When input sources or ground constraints are located at the bottom of the sketch (Y_source > Y_other),
        ///   returns true so that power spine flows upward from bottom to top (decreasing canvas Y).
        /// - Prevents vertical inversion where a bottom motor/source would otherwise be rendered at the top.
        /// </summary>
        public static bool ResolveFlowBottomToTop(SystemDiagram? diagram) {
            if (diagram == null) return false;
            return ResolveFlowBottomToTop(diagram.getElements());
        }

        /// <summary>
        /// Resolves whether a collection of SystemDiagram elements flows Bottom-to-Top along the Y-axis.
        /// </summary>
        public static bool ResolveFlowBottomToTop(IEnumerable<SystemDiagram.Element>? elements) {
            if (elements == null || !elements.Any()) return false;

            var elementList = elements.ToList();
            var groundElements = elementList.Where(e => {
                int t = e.getType();
                return t == 3 || t == 24 || (!string.IsNullOrEmpty(e.getName()) && e.getName().Contains("Ground"));
            }).ToList();

            var sourceElements = elementList.Where(e => {
                int t = e.getType();
                return t == 4 || t == 6 || t == 10 || t == 11 || t == 25 || t == 26 ||
                       (!string.IsNullOrEmpty(e.getName()) && (e.getName().Contains("Force") || e.getName().Contains("Input") || e.getName().Contains("Velocity")));
            }).ToList();

            if (groundElements.Count > 0 && sourceElements.Count > 0) {
                double avgGroundY = groundElements.Average(e => e.getY());
                double avgSourceY = sourceElements.Average(e => e.getY());
                // In canvas coordinates, larger Y is at the bottom; if ground is lower down than source, flow is Bottom-to-Top
                return avgGroundY > avgSourceY;
            }

            if (sourceElements.Count > 0) {
                double avgSourceY = sourceElements.Average(e => e.getY());
                double otherAvgY = elementList.Where(e => !sourceElements.Contains(e)).Select(e => e.getY()).DefaultIfEmpty(avgSourceY).Average();
                // If source has larger Y than other elements, source is at bottom and flows upward
                return avgSourceY > otherAvgY;
            }

            if (groundElements.Count > 0) {
                double avgGroundY = groundElements.Average(e => e.getY());
                double otherAvgY = elementList.Where(e => !groundElements.Contains(e)).Select(e => e.getY()).DefaultIfEmpty(avgGroundY).Average();
                return avgGroundY > otherAvgY;
            }

            // Fallback: compare first and last elements
            return elementList.First().getY() > elementList.Last().getY();
        }

        /// <summary>
        /// Checks if the graph is optimized
        /// </summary>
        /// <returns>True if the graph is optimized, false otherwise</returns>
        public bool isOptimized() {
            return this.optimized;
        }

        /// <summary>
        /// Gets the bond graph
        /// </summary>
        /// <returns>A bond graph</returns>
        public BondGraph getBondGraph() {
            return this.bondGraph;
        }

        /// <summary>
        /// Gets the configured orientation
        /// </summary>
        public LayoutOrientation getOrientation() {
            return this.orientation;
        }

        /// <summary>
        /// Gets whether the layout flows left-to-right
        /// </summary>
        public bool isFlowLeftToRight() {
            return this.flowLeftToRight;
        }

        /// <summary>
        /// Gets whether the layout flows bottom-to-top
        /// </summary>
        public bool isFlowBottomToTop() {
            return this.flowBottomToTop;
        }

        /// <summary>
        /// Creates an orthogonal grid embedding of the bond graph.
        /// All bonds are aligned strictly horizontally or vertically.
        /// </summary>
        public void embedBondGraph() {
            Dictionary<string, BondGraph.Element> elementsDict = this.bondGraph.getElements();
            if (elementsDict == null || elementsDict.Count == 0) {
                this.optimized = true;
                return;
            }

            List<BondGraph.Element> elements = elementsDict.Values.ToList();
            if (elements.Count == 1) {
                elements[0].setPosition(1000.0, 1000.0);
                this.optimized = true;
                return;
            }

            List<BondGraph.Bond> bonds = this.bondGraph.getBonds();

            // Build adjacency map
            Dictionary<BondGraph.Element, List<BondGraph.Element>> adj = new();
            foreach (BondGraph.Element el in elements) {
                adj[el] = new List<BondGraph.Element>();
            }

            foreach (BondGraph.Bond bond in bonds) {
                BondGraph.Element src = bond.getSource();
                BondGraph.Element sink = bond.getSink();
                if (src != null && sink != null && !src.Equals(sink)) {
                    if (adj.ContainsKey(src) && !adj[src].Contains(sink)) {
                        adj[src].Add(sink);
                    }
                    if (adj.ContainsKey(sink) && !adj[sink].Contains(src)) {
                        adj[sink].Add(src);
                    }
                }
            }

            // Find connected components
            HashSet<BondGraph.Element> visited = new();
            List<List<BondGraph.Element>> components = new();
            foreach (BondGraph.Element el in elements) {
                if (visited.Contains(el)) continue;

                List<BondGraph.Element> comp = new();
                Queue<BondGraph.Element> q = new();
                q.Enqueue(el);
                visited.Add(el);

                while (q.Count > 0) {
                    BondGraph.Element cur = q.Dequeue();
                    comp.Add(cur);
                    foreach (BondGraph.Element nbr in adj[cur]) {
                        if (!visited.Contains(nbr)) {
                            visited.Add(nbr);
                            q.Enqueue(nbr);
                        }
                    }
                }
                components.Add(comp);
            }

            // Lay out each component on an integer grid
            Dictionary<BondGraph.Element, (int gx, int gy)> nodeGrid = new();
            HashSet<(int gx, int gy)> occupiedGrid = new();
            List<((int x1, int y1), (int x2, int y2))> edgeSegments = new();

            int currentComponentStartX = 0;
            int currentComponentStartY = 0;

            foreach (List<BondGraph.Element> comp in components) {
                LayoutComponent(comp, adj, nodeGrid, occupiedGrid, edgeSegments, this.orientation, this.flowLeftToRight, this.flowBottomToTop, ref currentComponentStartX, ref currentComponentStartY);
            }

            // Convert grid coordinates to screen coordinates
            if (nodeGrid.Count > 0) {
                int minGx = nodeGrid.Values.Min(p => p.gx);
                int maxGx = nodeGrid.Values.Max(p => p.gx);
                int minGy = nodeGrid.Values.Min(p => p.gy);
                int maxGy = nodeGrid.Values.Max(p => p.gy);

                double centerX = (minGx + maxGx) * 0.5 * GridSpacingX;
                double centerY = (minGy + maxGy) * 0.5 * GridSpacingY;
                const double targetCenterX = 1000.0;
                const double targetCenterY = 1000.0;

                foreach (KeyValuePair<BondGraph.Element, (int gx, int gy)> kvp in nodeGrid) {
                    BondGraph.Element el = kvp.Key;
                    (int gx, int gy) = kvp.Value;
                    double sx = (gx * GridSpacingX) - centerX + targetCenterX;
                    double sy = (gy * GridSpacingY) - centerY + targetCenterY;
                    el.setPosition(sx, sy);
                }
            }

            this.optimized = true;
        }

        private static bool IsCore(BondGraph.Element e) {
            string l = e.label ?? "";
            return l == "0" || l == "1" || l.StartsWith("0 ") || l.StartsWith("1 ") || l.StartsWith("1GND") || l.StartsWith("TF") || l.StartsWith("GY");
        }

        private static string GetBaseLabel(string? label) {
            if (string.IsNullOrEmpty(label)) return "";
            if (label == "0" || label.StartsWith("0 ")) return "0";
            if (label == "1" || label.StartsWith("1 ") || label.StartsWith("1GND")) return "1";
            int colonIdx = label.IndexOf(':');
            return colonIdx >= 0 ? label.Substring(0, colonIdx) : label;
        }

        /// <summary>
        /// Causal Coordinate Parity: Locks causal bond graph node coordinates directly to the
        /// corresponding coordinates already solved for the simplified bond graph.
        /// 
        /// Why Direct Inheritance is Critical:
        /// A causal bond graph shares the exact same topological structure and components as the simplified graph;
        /// it only adds causal strokes (perpendicular bars) and effort/flow variable annotations.
        /// Running an independent layout solver (e.g., an unseeded force-directed simulation or unconstrained grid pass)
        /// causes severe diagonal drift, ray starbursting, and line intersections. Strictly inheriting coordinates
        /// guarantees 100% geometric and topological consistency across both views.
        /// </summary>
        public static void InheritLayout(BondGraph targetCausalGraph, BondGraph? sourceSimplifiedGraph, LayoutOrientation orientation = LayoutOrientation.Horizontal, bool flowLeftToRight = true, bool flowBottomToTop = false) {
            if (targetCausalGraph == null) return;

            var targetElements = targetCausalGraph.getElements();
            if (targetElements == null || targetElements.Count == 0) return;

            var sourceElements = sourceSimplifiedGraph?.getElements();
            if (sourceElements == null || sourceElements.Count == 0) {
                // Fallback: embed directly
                BondGraphEmbedder fallbackEmbedder = new(targetCausalGraph, orientation, flowLeftToRight, flowBottomToTop);
                fallbackEmbedder.embedBondGraph();
                return;
            }

            HashSet<string> unplacedTargetKeys = new(targetElements.Keys);

            // Step 1: Exact element name match
            foreach (var kvp in targetElements) {
                string key = kvp.Key;
                BondGraph.Element targetEl = kvp.Value;

                if (sourceElements.TryGetValue(key, out BondGraph.Element? sourceEl) && sourceEl != null) {
                    targetEl.setPosition(sourceEl.getX(), sourceEl.getY());
                    unplacedTargetKeys.Remove(key);
                } else {
                    // Try match by getName()
                    var matchByName = sourceElements.Values.FirstOrDefault(se => se.getName() == targetEl.getName());
                    if (matchByName != null) {
                        targetEl.setPosition(matchByName.getX(), matchByName.getY());
                        unplacedTargetKeys.Remove(key);
                    }
                }
            }

            // Step 2: If any nodes remain unplaced (e.g. key/name variations), match by normalized role/label
            if (unplacedTargetKeys.Count > 0) {
                var remainingSourceElements = sourceElements.Values
                    .Where(se => !targetElements.Values.Any(te => !unplacedTargetKeys.Contains(te.getName()) && Math.Abs(te.getX() - se.getX()) < 0.001 && Math.Abs(te.getY() - se.getY()) < 0.001))
                    .ToList();

                foreach (string unplacedKey in unplacedTargetKeys.ToList()) {
                    var targetEl = targetElements[unplacedKey];
                    string targetBaseLabel = GetBaseLabel(targetEl.label);

                    var sourceCandidate = remainingSourceElements.FirstOrDefault(se => GetBaseLabel(se.label) == targetBaseLabel);
                    if (sourceCandidate != null) {
                        targetEl.setPosition(sourceCandidate.getX(), sourceCandidate.getY());
                        remainingSourceElements.Remove(sourceCandidate);
                        unplacedTargetKeys.Remove(unplacedKey);
                    }
                }
            }

            // Step 3: If any nodes still unplaced, place them adjacent to their placed neighbors
            if (unplacedTargetKeys.Count > 0) {
                var bonds = targetCausalGraph.getBonds();
                foreach (string unplacedKey in unplacedTargetKeys.ToList()) {
                    var targetEl = targetElements[unplacedKey];
                    var placedNeighbor = bonds
                        .Where(b => b.getSource() == targetEl || b.getSink() == targetEl)
                        .Select(b => b.getSource() == targetEl ? b.getSink() : b.getSource())
                        .FirstOrDefault(n => n != null && !unplacedTargetKeys.Contains(n.getName()));

                    if (placedNeighbor != null) {
                        targetEl.setPosition(placedNeighbor.getX(), placedNeighbor.getY() + GridSpacingY);
                        unplacedTargetKeys.Remove(unplacedKey);
                    } else {
                        targetEl.setPosition(1000.0, 1000.0);
                        unplacedTargetKeys.Remove(unplacedKey);
                    }
                }
            }
        }

        private static bool IsSource(BondGraph.Element e) {
            string l = e.label ?? "";
            return l.StartsWith("Se") || l.StartsWith("Sf");
        }

        private static bool IsGround(BondGraph.Element e) {
            string l = e.label ?? "";
            return l.StartsWith("1GND") || l.Contains("GND");
        }

        private static bool IsInlineSpineElement(BondGraph.Element e) {
            string l = e.label ?? "";
            return l.StartsWith("TF") || l.StartsWith("GY");
        }

        private static void LayoutComponent(
            List<BondGraph.Element> comp,
            Dictionary<BondGraph.Element, List<BondGraph.Element>> adj,
            Dictionary<BondGraph.Element, (int gx, int gy)> nodeGrid,
            HashSet<(int gx, int gy)> occupiedGrid,
            List<((int x1, int y1), (int x2, int y2))> edgeSegments,
            LayoutOrientation orientation,
            bool flowLeftToRight,
            bool flowBottomToTop,
            ref int currentComponentStartX,
            ref int currentComponentStartY) {

            HashSet<BondGraph.Element> compSet = new(comp);

            // Forward and lateral orthogonal vectors based on orientation
            (int fX, int fY) forwardDir;
            (int o1X, int o1Y) ortho1;
            (int o2X, int o2Y) ortho2;
            (int bX, int bY) backwardDir;

            if (orientation == LayoutOrientation.Horizontal || orientation == LayoutOrientation.ElectricalHybrid) {
                // Horizontal flow: Left-to-Right along X
                forwardDir = (1, 0);
                ortho1 = (0, -1);      // North / Up
                ortho2 = (0, 1);       // South / Down
                backwardDir = (-1, 0);  // West / Left
            } else if (flowBottomToTop) {
                // Vertical flow: Bottom-to-Top along Y (decreasing Y)
                forwardDir = (0, -1);   // North / Up (decreasing Y)
                backwardDir = (0, 1);   // South / Down (increasing Y)
                ortho1 = (-1, 0);       // West / Left
                ortho2 = (1, 0);        // East / Right
            } else {
                // Vertical flow: Top-to-Bottom along Y (increasing Y)
                forwardDir = (0, 1);    // South / Down (increasing Y)
                backwardDir = (0, -1);  // North / Up (decreasing Y)
                ortho1 = (-1, 0);       // West / Left
                ortho2 = (1, 0);        // East / Right
            }

            // -------------------------------------------------------------
            // ROOT SELECTION MATCHING PHYSICAL BOUNDARY SEQUENCE
            // -------------------------------------------------------------
            // When flow is Left-to-Right, the leftmost node (Ground 1GND or ground-side mass junction)
            // must be the layout root, placing Ground on the left and Force on the right.
            // When flow is Bottom-to-Top, the bottom-most boundary node (Source or Ground at the bottom)
            // must be the layout root, placing Source at the bottom and advancing upward.
            BondGraph.Element? root = null;
            BondGraph.Element? sourceNode = comp.FirstOrDefault(IsSource);
            BondGraph.Element? groundNode = comp.FirstOrDefault(IsGround);

            if (orientation == LayoutOrientation.Horizontal) {
                if (flowLeftToRight) {
                    if (groundNode != null) {
                        root = groundNode;
                    } else if (sourceNode != null) {
                        // In simplified/causal graphs without 1GND, find the core junction furthest from sourceNode
                        root = FindFurthestCoreNode(sourceNode, compSet, adj);
                    }
                } else if (sourceNode != null) {
                    BondGraph.Element? adjacentCore = adj[sourceNode].FirstOrDefault(n => compSet.Contains(n) && IsCore(n));
                    root = adjacentCore ?? sourceNode;
                }
            } else if (orientation == LayoutOrientation.Vertical) {
                if (flowBottomToTop) {
                    if (sourceNode != null) {
                        BondGraph.Element? adjacentCore = adj[sourceNode].FirstOrDefault(n => compSet.Contains(n) && IsCore(n));
                        root = adjacentCore ?? sourceNode;
                    } else if (groundNode != null) {
                        root = groundNode;
                    }
                } else {
                    if (groundNode != null) {
                        root = groundNode;
                    } else if (sourceNode != null) {
                        BondGraph.Element? adjacentCore = adj[sourceNode].FirstOrDefault(n => compSet.Contains(n) && IsCore(n));
                        root = adjacentCore ?? sourceNode;
                    }
                }
            } else if (sourceNode != null) {
                BondGraph.Element? adjacentCore = adj[sourceNode].FirstOrDefault(n => compSet.Contains(n) && IsCore(n));
                root = adjacentCore ?? sourceNode;
            }

            if (root == null) {
                root = comp.Where(IsCore).OrderByDescending(e => adj[e].Count).FirstOrDefault();
            }

            if (root == null) {
                root = comp.OrderByDescending(e => adj[e].Count).First();
            }

            // Calculate subtree sizes prioritizing heavy core paths
            Dictionary<BondGraph.Element, int> coreSubtreeSizes = new();
            HashSet<BondGraph.Element> visitedForSize = new();
            CalculateSubtreeSizes(root, null, adj, compSet, coreSubtreeSizes, visitedForSize);

            // Place root
            int rootGx = currentComponentStartX;
            int rootGy = currentComponentStartY;
            while (occupiedGrid.Contains((rootGx, rootGy))) {
                if (orientation == LayoutOrientation.Horizontal) {
                    rootGx += 2;
                } else {
                    rootGy += 2;
                }
            }

            nodeGrid[root] = (rootGx, rootGy);
            occupiedGrid.Add((rootGx, rootGy));

            // Queue for layout: (element, parent, incomingDirX, incomingDirY)
            Queue<(BondGraph.Element node, BondGraph.Element? parent, int inDirX, int inDirY)> queue = new();
            HashSet<BondGraph.Element> placed = new() { root };

            queue.Enqueue((root, null, forwardDir.fX, forwardDir.fY));

            while (queue.Count > 0) {
                var (cur, parent, inDirX, inDirY) = queue.Dequeue();
                var (curX, curY) = nodeGrid[cur];

                // Get unplaced neighbors
                List<BondGraph.Element> unplacedNeighbors = adj[cur]
                    .Where(n => compSet.Contains(n) && !placed.Contains(n))
                    .ToList();

                if (unplacedNeighbors.Count == 0) continue;

                List<BondGraph.Element> coreChildren = unplacedNeighbors
                    .Where(IsCore)
                    .OrderByDescending(c => coreSubtreeSizes.GetValueOrDefault(c, 0))
                    .ThenByDescending(c => adj[c].Count)
                    .ToList();

                List<BondGraph.Element> leafChildren = unplacedNeighbors
                    .Where(c => !IsCore(c))
                    .OrderByDescending(IsSource)
                    .ThenBy(c => c.label)
                    .ToList();

                // -------------------------------------------------------------
                // 1. PLACE ATTACHED LEAVES OF 'CUR' PERPENDICULARLY (OUTWARD STUBS)
                // -------------------------------------------------------------
                // Attached 1-port leaves (I, R, C, Se, Sf) branch into distinct orthogonal
                // directions around 'cur' so no bond segment crosses another element.
                if (leafChildren.Count > 0) {
                    var candidateDirs = GetPreferredLeafDirections(cur, parent, nodeGrid, forwardDir, ortho1, ortho2, backwardDir, coreChildren.Count > 0);
                    HashSet<(int dx, int dy)> usedDirsOnCur = new();

                    if (parent != null && nodeGrid.TryGetValue(parent, out var pPos)) {
                        int backX = Math.Sign(pPos.gx - curX);
                        int backY = Math.Sign(pPos.gy - curY);
                        usedDirsOnCur.Add((backX, backY));
                    }
                    if (coreChildren.Count > 0) {
                        usedDirsOnCur.Add((forwardDir.fX, forwardDir.fY));
                    }

                    foreach (var leaf in leafChildren) {
                        (int dx, int dy) chosenLeafDir = (0, 0);
                        bool found = false;

                        // Pass 1: find direction that is both unused on cur and unoccupied on grid at distance 1
                        foreach (var dir in candidateDirs) {
                            if (!usedDirsOnCur.Contains(dir) && !occupiedGrid.Contains((curX + dir.dx, curY + dir.dy))) {
                                chosenLeafDir = dir;
                                found = true;
                                break;
                            }
                        }

                        // Pass 2: find any direction not used on cur
                        if (!found) {
                            foreach (var dir in candidateDirs) {
                                if (!usedDirsOnCur.Contains(dir)) {
                                    chosenLeafDir = dir;
                                    found = true;
                                    break;
                                }
                            }
                        }

                        // Pass 3: fallback
                        if (!found) {
                            chosenLeafDir = FindBestAvailableDirection(curX, curY, occupiedGrid);
                        }

                        usedDirsOnCur.Add(chosenLeafDir);

                        int leafX = curX + chosenLeafDir.dx;
                        int leafY = curY + chosenLeafDir.dy;

                        while (occupiedGrid.Contains((leafX, leafY))) {
                            leafX += chosenLeafDir.dx;
                            leafY += chosenLeafDir.dy;
                        }

                        nodeGrid[leaf] = (leafX, leafY);
                        occupiedGrid.Add((leafX, leafY));
                        edgeSegments.Add(((curX, curY), (leafX, leafY)));
                        placed.Add(leaf);
                        unplacedNeighbors.Remove(leaf);
                    }
                }

                if (unplacedNeighbors.Count == 0) continue;

                // -------------------------------------------------------------
                // 2. SYMMETRIC DIAMOND TOPOLOGY FOR PARALLEL BRANCHES
                // -------------------------------------------------------------
                if (coreChildren.Count >= 2) {
                    var parallelPair = FindParallelCorePair(cur, coreChildren, adj, compSet, placed);
                    if (parallelPair != null) {
                        var (b1, b2, targetJ2) = parallelPair.Value;

                        // Place B1 symmetrically on ortho1 side
                        int b1Gx = curX + forwardDir.fX + ortho1.o1X;
                        int b1Gy = curY + forwardDir.fY + ortho1.o1Y;

                        // Place B2 symmetrically on ortho2 side
                        int b2Gx = curX + forwardDir.fX + ortho2.o2X;
                        int b2Gy = curY + forwardDir.fY + ortho2.o2Y;

                        // Place targetJ2 along the primary forward spine
                        int j2Gx = curX + (2 * forwardDir.fX);
                        int j2Gy = curY + (2 * forwardDir.fY);

                        // If any slot is occupied, shift forward
                        while (occupiedGrid.Contains((b1Gx, b1Gy)) || occupiedGrid.Contains((b2Gx, b2Gy)) || occupiedGrid.Contains((j2Gx, j2Gy))) {
                            curX += forwardDir.fX;
                            curY += forwardDir.fY;
                            b1Gx = curX + forwardDir.fX + ortho1.o1X;
                            b1Gy = curY + forwardDir.fY + ortho1.o1Y;
                            b2Gx = curX + forwardDir.fX + ortho2.o2X;
                            b2Gy = curY + forwardDir.fY + ortho2.o2Y;
                            j2Gx = curX + (2 * forwardDir.fX);
                            j2Gy = curY + (2 * forwardDir.fY);
                        }

                        // Register B1
                        nodeGrid[b1] = (b1Gx, b1Gy);
                        occupiedGrid.Add((b1Gx, b1Gy));
                        edgeSegments.Add(((curX, curY), (b1Gx, b1Gy)));
                        placed.Add(b1);

                        // Register B2
                        nodeGrid[b2] = (b2Gx, b2Gy);
                        occupiedGrid.Add((b2Gx, b2Gy));
                        edgeSegments.Add(((curX, curY), (b2Gx, b2Gy)));
                        placed.Add(b2);

                        // Register target J2
                        nodeGrid[targetJ2] = (j2Gx, j2Gy);
                        occupiedGrid.Add((j2Gx, j2Gy));
                        edgeSegments.Add(((b1Gx, b1Gy), (j2Gx, j2Gy)));
                        edgeSegments.Add(((b2Gx, b2Gy), (j2Gx, j2Gy)));
                        placed.Add(targetJ2);

                        // Place 1-port leaves attached to B1 outward along ortho1
                        var b1Leaves = adj[b1].Where(n => compSet.Contains(n) && !IsCore(n) && !placed.Contains(n)).ToList();
                        for (int li = 0; li < b1Leaves.Count; li++) {
                            var leaf = b1Leaves[li];
                            int leafGx = b1Gx + ((li + 1) * ortho1.o1X);
                            int leafGy = b1Gy + ((li + 1) * ortho1.o1Y);
                            nodeGrid[leaf] = (leafGx, leafGy);
                            occupiedGrid.Add((leafGx, leafGy));
                            edgeSegments.Add(((b1Gx, b1Gy), (leafGx, leafGy)));
                            placed.Add(leaf);
                        }

                        // Place 1-port leaves attached to B2 outward along ortho2
                        var b2Leaves = adj[b2].Where(n => compSet.Contains(n) && !IsCore(n) && !placed.Contains(n)).ToList();
                        for (int li = 0; li < b2Leaves.Count; li++) {
                            var leaf = b2Leaves[li];
                            int leafGx = b2Gx + ((li + 1) * ortho2.o2X);
                            int leafGy = b2Gy + ((li + 1) * ortho2.o2Y);
                            nodeGrid[leaf] = (leafGx, leafGy);
                            occupiedGrid.Add((leafGx, leafGy));
                            edgeSegments.Add(((b2Gx, b2Gy), (leafGx, leafGy)));
                            placed.Add(leaf);
                        }

                        // Remove placed from lists and enqueue targetJ2 to continue along the spine
                        unplacedNeighbors.Remove(b1);
                        unplacedNeighbors.Remove(b2);
                        coreChildren.Remove(b1);
                        coreChildren.Remove(b2);
                        queue.Enqueue((targetJ2, cur, forwardDir.fX, forwardDir.fY));
                    }
                }

                if (unplacedNeighbors.Count == 0) continue;

                // -------------------------------------------------------------
                // 3. CARDINAL PORT ASSIGNMENT FOR REMAINING CORE CHILDREN
                // -------------------------------------------------------------
                // The primary core child continues forward along the spine
                foreach (var child in unplacedNeighbors.ToList()) {
                    if (placed.Contains(child)) continue;

                    (int dirX, int dirY) chosenDir = forwardDir;
                    bool isInlineElement = IsInlineSpineElement(cur) || IsInlineSpineElement(child);
                    int step = isInlineElement ? 1 : 2;

                    // If forward direction at step is occupied, pick an open lateral direction
                    if (occupiedGrid.Contains((curX + (step * forwardDir.fX), curY + (step * forwardDir.fY)))) {
                        if (!occupiedGrid.Contains((curX + ortho1.o1X, curY + ortho1.o1Y))) {
                            chosenDir = ortho1;
                            step = 1;
                        } else if (!occupiedGrid.Contains((curX + ortho2.o2X, curY + ortho2.o2Y))) {
                            chosenDir = ortho2;
                            step = 1;
                        } else {
                            chosenDir = FindBestAvailableDirection(curX, curY, occupiedGrid);
                            step = 1;
                        }
                    }

                    int targetGx = curX + (step * chosenDir.dirX);
                    int targetGy = curY + (step * chosenDir.dirY);

                    // Dedicated placement without ray-hopping
                    while (occupiedGrid.Contains((targetGx, targetGy))) {
                        targetGx += chosenDir.dirX;
                        targetGy += chosenDir.dirY;
                    }

                    nodeGrid[child] = (targetGx, targetGy);
                    occupiedGrid.Add((targetGx, targetGy));
                    edgeSegments.Add(((curX, curY), (targetGx, targetGy)));
                    placed.Add(child);

                    queue.Enqueue((child, cur, chosenDir.dirX, chosenDir.dirY));
                }
            }

            // Update start coordinates for next disconnected component
            if (orientation == LayoutOrientation.Horizontal) {
                int maxCompGx = comp.Where(nodeGrid.ContainsKey).Select(e => nodeGrid[e].gx).DefaultIfEmpty(currentComponentStartX).Max();
                currentComponentStartX = maxCompGx + 3;
            } else if (flowBottomToTop) {
                int minCompGy = comp.Where(nodeGrid.ContainsKey).Select(e => nodeGrid[e].gy).DefaultIfEmpty(currentComponentStartY).Min();
                currentComponentStartY = minCompGy - 3;
            } else {
                int maxCompGy = comp.Where(nodeGrid.ContainsKey).Select(e => nodeGrid[e].gy).DefaultIfEmpty(currentComponentStartY).Max();
                currentComponentStartY = maxCompGy + 3;
            }
        }

        /// <summary>
        /// Orthogonal Port Allocator (Collision Avoidance):
        /// Computes ordered candidate cardinal directions (Up, Down, Left, Right) for attached 1-port leaves (I, R, C, Se, Sf).
        /// Prioritizes perpendicular ports relative to the incoming parent spine bond, then forward/backward ports,
        /// ensuring each attached element receives a dedicated outward ray rather than collinear stacking.
        /// </summary>
        private static List<(int dx, int dy)> GetPreferredLeafDirections(
            BondGraph.Element cur,
            BondGraph.Element? parent,
            Dictionary<BondGraph.Element, (int gx, int gy)> nodeGrid,
            (int fX, int fY) forwardDir,
            (int o1X, int o1Y) ortho1,
            (int o2X, int o2Y) ortho2,
            (int bX, int bY) backwardDir,
            bool hasCoreChildren) {

            List<(int dx, int dy)> dirs = new();

            if (parent != null && nodeGrid.TryGetValue(parent, out var pPos) && nodeGrid.TryGetValue(cur, out var cPos)) {
                int backX = Math.Sign(pPos.gx - cPos.gx);
                int backY = Math.Sign(pPos.gy - cPos.gy);
                // Orthogonal to incoming parent bond
                (int p1X, int p1Y) = (-backY, backX);
                (int p2X, int p2Y) = (backY, -backX);
                (int fwdX, int fwdY) = (-backX, -backY);

                dirs.Add((p1X, p1Y));
                dirs.Add((fwdX, fwdY));
                dirs.Add((p2X, p2Y));
            } else {
                dirs.Add(ortho1);
                dirs.Add(ortho2);
                dirs.Add(backwardDir);
                if (!hasCoreChildren) {
                    dirs.Add(forwardDir);
                }
            }

            (int dx, int dy)[] allCardinals = new[] { ortho1, ortho2, backwardDir, forwardDir };
            foreach (var d in allCardinals) {
                if (!dirs.Contains(d)) {
                    dirs.Add(d);
                }
            }

            return dirs;
        }

        private static BondGraph.Element FindFurthestCoreNode(
            BondGraph.Element source,
            HashSet<BondGraph.Element> compSet,
            Dictionary<BondGraph.Element, List<BondGraph.Element>> adj) {

            // BFS from source to find the core node with maximum graph distance
            Queue<(BondGraph.Element node, int dist)> q = new();
            HashSet<BondGraph.Element> visited = new() { source };
            q.Enqueue((source, 0));

            BondGraph.Element furthest = source;
            int maxDist = 0;

            while (q.Count > 0) {
                var (cur, dist) = q.Dequeue();
                if (IsCore(cur) && dist > maxDist) {
                    maxDist = dist;
                    furthest = cur;
                }

                foreach (var nbr in adj[cur]) {
                    if (compSet.Contains(nbr) && !visited.Contains(nbr)) {
                        visited.Add(nbr);
                        q.Enqueue((nbr, dist + 1));
                    }
                }
            }

            return furthest;
        }

        /// <summary>
        /// Parallel Diamond Construction: Identifies two parallel branches (e.g. spring K and damper b)
        /// connecting two common 1-junctions and routes them as a symmetric diamond.
        /// 
        /// Geometric Construction:
        /// - Identifies unplaced common core neighbors (b1, b2) bridging junction 'cur' and 'targetJ2'.
        /// - Branch 1 (b1) and Branch 2 (b2) are placed symmetrically along ortho1 (North/Left) and ortho2 (South/Right).
        /// - The recombining junction (targetJ2) is placed along the primary forward spine.
        /// - Attached 1-port leaves extend outward from their respective 0-junctions, maintaining bilateral symmetry.
        /// </summary>
        private static (BondGraph.Element b1, BondGraph.Element b2, BondGraph.Element targetJ2)? FindParallelCorePair(
            BondGraph.Element cur,
            List<BondGraph.Element> coreChildren,
            Dictionary<BondGraph.Element, List<BondGraph.Element>> adj,
            HashSet<BondGraph.Element> compSet,
            HashSet<BondGraph.Element> placed) {

            for (int i = 0; i < coreChildren.Count; i++) {
                for (int j = i + 1; j < coreChildren.Count; j++) {
                    var c1 = coreChildren[i];
                    var c2 = coreChildren[j];

                    // Find unplaced common core neighbor of c1 and c2 (other than cur)
                    var commonTargets = adj[c1]
                        .Where(n => compSet.Contains(n) && IsCore(n) && !n.Equals(cur) && !placed.Contains(n) && adj[c2].Contains(n))
                        .ToList();

                    if (commonTargets.Count > 0) {
                        return (c1, c2, commonTargets[0]);
                    }
                }
            }

            return null;
        }

        private static int CalculateSubtreeSizes(
            BondGraph.Element cur,
            BondGraph.Element? parent,
            Dictionary<BondGraph.Element, List<BondGraph.Element>> adj,
            HashSet<BondGraph.Element> compSet,
            Dictionary<BondGraph.Element, int> sizes,
            HashSet<BondGraph.Element> visited) {

            visited.Add(cur);
            int count = IsCore(cur) ? 1 : 0;

            foreach (BondGraph.Element nbr in adj[cur]) {
                if (compSet.Contains(nbr) && !visited.Contains(nbr) && !nbr.Equals(parent)) {
                    count += CalculateSubtreeSizes(nbr, cur, adj, compSet, sizes, visited);
                }
            }

            sizes[cur] = count;
            return count;
        }

        private static (int dx, int dy) FindBestAvailableDirection(
            int curX, int curY, HashSet<(int gx, int gy)> occupiedGrid) {

            (int dx, int dy)[] cardinals = new[] {
                (0, -1),  // North
                (0, 1),   // South
                (1, 0),   // East
                (-1, 0)   // West
            };

            foreach (var (dx, dy) in cardinals) {
                if (!occupiedGrid.Contains((curX + dx, curY + dy))) {
                    return (dx, dy);
                }
            }

            return (0, -1);
        }
    }
}
