import { Category } from "./Category";
import { ElementType } from "./ElementType";

namespace ElementNamespace {
    let categories = [
        new Category("Basic Mechanical Translation", "mechTrans"),
        new Category("Basic Mechanical Rotation", "mechRot"),
        new Category("Transmission Elements", "transElem"),
        new Category("Electrical", "electrical"),
        new Category("Actuators", "actuators")
    ];

    let elementTypes: ElementType[] = [
        new ElementType(0, "Mass", 0, []),
        new ElementType(1, "Spring", 0, []),
        new ElementType(2, "Damper", 0, []),
        new ElementType(3, "Ground", 0, []),
        new ElementType(4, "Force Input", 0, []),
        new ElementType(5, "Gravity", 0, []),
        new ElementType(6, "Velocity Input", 0, []),
        new ElementType(7, "Flywheel", 1, []),
        new ElementType(8, "Rotational Spring", 1, []),
        new ElementType(9, "Damper", 1, []),
        new ElementType(10, "Torque Input", 1, []),
        new ElementType(11, "Velocity Input", 1, []),
        new ElementType(12, "Lever", 2, []),
        new ElementType(13, "Pulley", 2, []),
        new ElementType(14, "Belt", 2, []),
        new ElementType(15, "Shaft", 2, []),
        new ElementType(16, "Gear", 2, []),
        new ElementType(17, "Gear Pair", 2, []),
        new ElementType(18, "Rack", 2, []),
        new ElementType(19, "Rack Pinion", 2, []),
        new ElementType(20, "Inductor", 3, []),
        new ElementType(21, "Capacitor", 3, []),
        new ElementType(22, "Resistor", 3, []),
        new ElementType(23, "Transformer", 3, []),
        new ElementType(24, "Junction Palette", 3, []),
        new ElementType(25, "Ground", 3, []),
        new ElementType(26, "Current Input", 3, []),
        new ElementType(27, "Voltage Input", 3, []),
        new ElementType(28, "PM Motor", 4, []),
        new ElementType(29, "VC Transducer", 4, [])
    ];
}