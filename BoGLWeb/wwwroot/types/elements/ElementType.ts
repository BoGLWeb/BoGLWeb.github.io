﻿export class ElementType {
    id: number;
    name: string;
    category: number;
    allowedModifiers: number[];
    image: string;
    velocityAllowed: boolean;

    constructor(id: number, name: string, category: number, image: string, allowedModifiers: number[], velocityAllowed: boolean, maxConnections: number = Number.MAX_SAFE_INTEGER) {
        this.id = id;
        this.name = name;
        this.category = category;
        this.allowedModifiers = allowedModifiers;
        this.image = image;
        this.velocityAllowed = velocityAllowed;
    }
}