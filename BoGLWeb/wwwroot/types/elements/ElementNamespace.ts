import { SystemDiagramDisplay } from "../display/SystemDiagramDisplay";
import { Category } from "./Category";
import { ElementType } from "./ElementType";
import { Modifier } from "./Modifier";
import { SystemDiagramElement } from "./SystemDiagramElement";
import {MultiElementType} from "./MultiElementType";

// provides information about all system diagram elements
export namespace ElementNamespace {
    // system diagram element categories
    export const categories: Category[] = [
        new Category(0, "Basic Mechanical Translation", "mechTrans"),
        new Category(1, "Basic Mechanical Rotation", "mechRot"),
        new Category(2, "Transmission Elements", "transElem"),
        new Category(3, "Electrical", "electrical"),
        new Category(4, "Actuators", "actuators")
    ];

    // list of element modifiers
    export const modifiers: Modifier[] = [
        new Modifier(0, "Mass"),
        new Modifier(1, "Inertia"),
        new Modifier(2, "Stiffness"),
        new Modifier(3, "Friction"),
        new Modifier(4, "Damping"),
        new Modifier(5, "Parallel"),
        new Modifier(6, "Tooth Wear")
    ];

    // all system diagram element types
    export const elementTypes: ElementType[] = [
        new ElementType(0, "Mass", 0, "mass", [3], true),
        new ElementType(1, "Spring", 0, "transSpring", [5], true, 2),
        new ElementType(2, "Damper", 0, "damper", [5], true, 2),
        new ElementType(3, "Ground", 0, "mech_ground", [], false),
        new ElementType(4, "Force Input", 0, "force_input", [], true),
        new ElementType(5, "Gravity", 0, "gravity", [], false),
        new ElementType(6, "Velocity Input", 0, "velocity_input", [], true),
        new ElementType(7, "Flywheel", 1, "flywheel", [], true),
        new ElementType(8, "Torsional Spring", 1, "torSpring", [5], true, 2),
        new ElementType(9, "Rotational Damper", 1, "damper", [5], true, 2),
        new ElementType(10, "Torque Input", 1, "torque_input", [], true),
        new ElementType(11, "Velocity Input", 1, "omega_input", [], true),
        new ElementType(12, "Lever", 2, "lever", [3, 1], true),
        new ElementType(13, "Belt", 2, "belt", [5, 2, 4], true),
        new ElementType(14, "Shaft", 2, "shaft", [2, 4], true, 2),
        new ElementType(15, "Gear", 2, "gear", [3, 1, 6], true),
        new MultiElementType(16, "Gear Pair", 2, "gear_pair", [], false, [16, 16], [[0, 1]], [[0,0], [100, 0]]),
        new ElementType(17, "Rack", 2, "rack", [3, 1, 6, 0], true),
        new MultiElementType(18, "Rack Pinion", 2, "rack_pinion", [], false, [16, 18], [[0,1]], [[0,0],[0,100]]),
        new ElementType(19, "Inductor", 3, "inductor", [], false, 2),
        new ElementType(20, "Capacitor", 3, "capacitor", [], false, 2),
        new ElementType(21, "Resistor", 3, "resistor", [], false, 2),
        new ElementType(22, "Transformer", 3, "transformer", [], false, 4),
        new ElementType(23, "Junction Palette", 3, "junction_palette", [], false, 4),
        new ElementType(24, "Ground", 3, "elec_ground", [], false, 2),
        new ElementType(25, "Current Input", 3, "current_input", [], false),
        new ElementType(26, "Voltage Input", 3, "voltage_input", [], false),
        new ElementType(27, "PM Motor", 4, "pm_motor", [], false),
        new ElementType(28, "VC Transducer", 4, "vc_transducer", [], false),
        new ElementType(29, "Grounded Pulley", 2, "pulley_grounded", [3, 1], true),
        //new ElementType(30, "Bearing", 2, "bearing", [], false, 2)
    ];

    // compatibility groups with element IDs showing which elements can connect to each other
    export const mtCompatibilityGroup = new Set([0, 1, 2, 3, 4, 5, 6, 17, 12, 13]);
    export const mrCompatibilityGroup = new Set([8, 9, 7, 12, 14, 13, 10, 12, 11, 15, 17, 27]);
    export const eCompatibilityGroup = new Set([20, 21, 24, 23, 22, 19, 26, 25, 27]);
    export const oCompatibilityGroup = new Set([28, 27]);

    // checks whether two system diagram elements can be connected
    export function isCompatible(e1: SystemDiagramElement, e2: SystemDiagramElement, graph: SystemDiagramDisplay) {
        if (e1 === null || e2 === null || e1.id === e2.id) return false;
        let mtCompatible = mtCompatibilityGroup.has(e1.type) && mtCompatibilityGroup.has(e2.type);
        let mrCompatible = mrCompatibilityGroup.has(e1.type) && mrCompatibilityGroup.has(e2.type);
        let eCompatible = eCompatibilityGroup.has(e1.type) && eCompatibilityGroup.has(e2.type);
        let oCompatible = oCompatibilityGroup.has(e1.type) && oCompatibilityGroup.has(e2.type);
        let maxSourceBonds = ElementNamespace.elementTypes[e1.type].maxConnections;
        let maxTargetBonds = ElementNamespace.elementTypes[e2.type].maxConnections;
        let numTargetBonds = graph.bonds.filter(b => b.target.id == e2.id || b.source.id == e2.id).length;
        let numSourceBonds = graph.bonds.filter(b => b.target.id == e1.id || b.source.id == e1.id).length;
        let edgesLikeThisCount = graph.bonds.filter(b => (b.target.id == e1.id && b.source.id == e2.id) || (b.target.id == e2.id && b.source.id == e1.id)).length;
        return (mtCompatible || mrCompatible || eCompatible || oCompatible) && (numSourceBonds < maxSourceBonds) && (numTargetBonds < maxTargetBonds) && (edgesLikeThisCount === 0);
    }
}
