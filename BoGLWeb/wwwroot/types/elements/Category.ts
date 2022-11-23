export class Category {
    folderName: string;
    name: string;
    id: number;

    constructor(id: number, name: string, folderName: string) {
        this.id = id;
        this.folderName = folderName;
        this.name = name;
    }
}