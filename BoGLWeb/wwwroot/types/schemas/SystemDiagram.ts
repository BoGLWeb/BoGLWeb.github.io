export type Element = {
    name: string | null
    modifiers: {
        [k: string]: boolean
    } | null
    velocityDir: string | null
    [k: string]: unknown
} | null
export type Edge = {
    e1: Element
    e2: Element
    velocityDir: string
    [k: string]: unknown
} | null

export interface SystemDiagram{
    elements: Element[] | null
    edges: Edge[] | null
    header: {
        [k: string]: number
    } | null
    [k: string]: unknown
}
