// provides the index and ID for a submenu of the top menu bar and tracks whether its click actions have been added
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