import { SystemDiagramDisplay } from "../display/SystemDiagramDisplay";
import { Category } from "./Category";
import { ElementType } from "./ElementType";
import { Modifier } from "./Modifier";
import { SystemDiagramElement } from "./SystemDiagramElement";

export namespace ElementNamespace {
    export const categories: Category[] = [
        new Category(0, "Basic Mechanical Translation", "mechTrans"),
        new Category(1, "Basic Mechanical Rotation", "mechRot"),
        new Category(2, "Transmission Elements", "transElem"),
        new Category(3, "Electrical", "electrical"),
        new Category(4, "Actuators", "actuators")
    ];

    export const modifiers: Modifier[] = [
        new Modifier(0, "Mass"),
        new Modifier(1, "Inertia"),
        new Modifier(2, "Stiffness"),
        new Modifier(3, "Friction"),
        new Modifier(4, "Damping"),
        new Modifier(5, "Parallel"),
        new Modifier(6, "Tooth Wear")
    ];

    export const elementTypes: ElementType[] = [
        new ElementType(0, "Mass", 0, "mass", [3], true),
        new ElementType(1, "Spring", 0, "spring", [5], true, 2),
        new ElementType(2, "Damper", 0, "damper", [5], true, 2),
        new ElementType(3, "Ground", 0, "ground", [], false),
        new ElementType(4, "Force Input", 0, "force_input", [], true),
        new ElementType(5, "Gravity", 0, "gravity", [], false),
        new ElementType(6, "Velocity Input", 0, "velocity_input", [], true),
        new ElementType(7, "Flywheel", 1, "flywheel", [], true),
        new ElementType(8, "Rotational Spring", 1, "spring", [5], true, 2),
        new ElementType(9, "Rotational Damper", 1, "damper", [5], true, 2),
        new ElementType(10, "Torque Input", 1, "torque_input", [], true),
        new ElementType(11, "Velocity Input", 1, "velocity_input", [], true),
        new ElementType(12, "Lever", 2, "lever", [3, 1], true),
        new ElementType(13, "Pulley", 2, "pulley", [3, 1], true),
        new ElementType(14, "Belt", 2, "belt", [5, 2, 4], true),
        new ElementType(15, "Shaft", 2, "shaft", [2, 4], true, 2),
        new ElementType(16, "Gear", 2, "gear", [3, 1, 6], true),
        new ElementType(17, "Gear Pair", 2, "gear_pair", [], false),
        new ElementType(18, "Rack", 2, "rack", [3, 1, 6, 0], true),
        new ElementType(19, "Rack Pinion", 2, "rack_pinion", [], false),
        new ElementType(20, "Inductor", 3, "inductor", [], false, 2),
        new ElementType(21, "Capacitor", 3, "capacitor", [], false, 2),
        new ElementType(22, "Resistor", 3, "resistor", [], false, 2),
        new ElementType(23, "Transformer", 3, "transformer", [], false, 4),
        new ElementType(24, "Junction Palette", 3, "junction_palette", [], false, 4),
        new ElementType(25, "Ground", 3, "ground", [], false, 2),
        new ElementType(26, "Current Input", 3, "current_input", [], false),
        new ElementType(27, "Voltage Input", 3, "voltage_input", [], false),
        new ElementType(28, "PM Motor", 4, "pm_motor", [], false),
        new ElementType(29, "VC Transducer", 4, "vc_transducer", [], false)
    ];
    
    export const mtCompatibilityGroup = new Set([0, 1, 2, 3, 4, 5, 6, 18, 12, 13, 14]);
    export const mrCompatibilityGroup = new Set([8, 9, 7, 12, 13, 15, 14, 10, 12, 11, 16, 18, 28]);
    export const eCompatibilityGroup = new Set([21, 22, 25, 24, 23, 20, 27, 26, 28]);
    export const oCompatibilityGroup = new Set([29, 28]);

    export function isCompatible(e1: SystemDiagramElement, e2: SystemDiagramElement, graph: SystemDiagramDisplay) {
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
