export class SubmenuID {
    index: number;
    id: number;
    hasClickAction: boolean;

    constructor(index: number, id: number) {
        this.index = index;
        this.id = id;
        this.hasClickAction = false;
    }
}