import { Category } from "./Category";
import { ElementType } from "./ElementType";
import { Modifier } from "./Modifier";

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
        new ElementType(0, "Mass", 0, "mass", []),
        new ElementType(1, "Spring", 0, "spring", []),
        new ElementType(2, "Damper", 0, "damper", []),
        new ElementType(3, "Ground", 0, "ground", []),
        new ElementType(4, "Force Input", 0, "force_input", []),
        new ElementType(5, "Gravity", 0, "gravity", []),
        new ElementType(6, "Velocity Input", 0, "velocity_input", []),
        new ElementType(7, "Flywheel", 1, "flywheel", []),
        new ElementType(8, "Rotational Spring", 1, "spring", []),
        new ElementType(9, "Rotational Damper", 1, "damper", []),
        new ElementType(10, "Torque Input", 1, "torque_input", []),
        new ElementType(11, "Velocity Input", 1, "velocity_input", []),
        new ElementType(12, "Lever", 2, "lever", []),
        new ElementType(13, "Pulley", 2, "pulley", []),
        new ElementType(14, "Belt", 2, "belt", []),
        new ElementType(15, "Shaft", 2, "shaft", []),
        new ElementType(16, "Gear", 2, "gear", []),
        new ElementType(17, "Gear Pair", 2, "gear_pair", []),
        new ElementType(18, "Rack", 2, "rack", []),
        new ElementType(19, "Rack Pinion", 2, "rack_pinion", []),
        new ElementType(20, "Inductor", 3, "inductor", []),
        new ElementType(21, "Capacitor", 3, "capacitor", []),
        new ElementType(22, "Resistor", 3, "resistor", []),
        new ElementType(23, "Transformer", 3, "transformer", []),
        new ElementType(24, "Junction Palette", 3, "junction_palette", []),
        new ElementType(25, "Ground", 3, "ground", []),
        new ElementType(26, "Current Input", 3, "current_input", []),
        new ElementType(27, "Voltage Input", 3, "voltage_input", []),
        new ElementType(28, "PM Motor", 4, "pm_motor", []),
        new ElementType(29, "VC Transducer", 4, "vc_transducer", [])
    ];
}
