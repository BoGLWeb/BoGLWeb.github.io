// provides information about a category of system diagram elements
export class Category {
    folderName: string; // DOM ID for category
    name: string; // name for category
    id: number; // integer ID for category

    constructor(id: number, name: string, folderName: string) {
        this.id = id;
        this.folderName = folderName;
        this.name = name;
    }
}