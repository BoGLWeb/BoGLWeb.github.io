export type Element = {
    label: string | null
    value: number
    [k: string]: unknown
} | null
export type Bond = {
    source: Element
    sink: Element
    label: string | null
    flow: number
    effort: number
    [k: string]: unknown
} | null

export interface BondGraph{
    elements: Element[] | null
    bonds: Bond[] | null
    [k: string]: unknown
}
