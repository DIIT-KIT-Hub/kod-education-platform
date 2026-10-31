import { useLingui } from "@lingui/react/macro";
import { ChevronRight } from "lucide-react";
import { Link } from "react-router";
import type { UnitTreeNode } from "@/shared/api/schema";
import { unitTypeIcon } from "@/shared/lib/unitTypeIcons";
import layout from "@/shared/styles/layout.module.css";
import { Badge } from "@/shared/ui/Badge";
import { Icon } from "@/shared/ui/Icon";
import styles from "./UnitTree.module.css";

/**
 * Props of {@link UnitTree}.
 */
export interface UnitTreeProps {
    /** The top-level nodes. */
    readonly nodes: readonly UnitTreeNode[];
    /** Opens every branch (the tree shows search results). */
    readonly expanded: boolean;
}

/**
 * The organizational tree: organizations open, deeper branches folded until the user opens them.
 * @param props The props.
 * @returns The tree.
 */
export function UnitTree({ nodes, expanded }: UnitTreeProps) {
    return (
        <ul className={[styles.tree, layout.stagger].join(" ")}>
            {nodes.map((node) => (
                <Branch key={node.id} node={node} open expanded={expanded} />
            ))}
        </ul>
    );
}

/**
 * Props of {@link Branch}.
 */
interface BranchProps {
    /** The node. */
    readonly node: UnitTreeNode;
    /** Whether the branch starts open. */
    readonly open: boolean;
    /** Opens every branch. */
    readonly expanded: boolean;
}

/**
 * A node with its children.
 * @param props The props.
 * @returns The list item.
 */
function Branch({ node, open, expanded }: BranchProps) {
    if (node.children.length === 0) {
        return (
            <li>
                <span className={styles.row}>
                    <span className={styles.spacer} />
                    <NodeLabel node={node} />
                </span>
            </li>
        );
    }

    return (
        <li>
            <details open={open || expanded}>
                <summary className={styles.row}>
                    <Icon icon={ChevronRight} size="sm" className={styles.chevron} />
                    <NodeLabel node={node} />
                </summary>
                <ul>
                    {node.children.map((child) => (
                        <Branch key={child.id} node={child} open={false} expanded={expanded} />
                    ))}
                </ul>
            </details>
        </li>
    );
}

/**
 * The icon, name, type and staff count of a node.
 * @param props The node.
 * @returns The label.
 */
function NodeLabel({ node }: { readonly node: UnitTreeNode }) {
    const { t } = useLingui();
    return (
        <>
            <Icon icon={unitTypeIcon(node.type.icon)} />
            <Link className={styles.name} to={`/structure/${node.id}`}>
                {node.name}
            </Link>
            <span className={styles.type}>{node.type.name}</span>
            {node.staffCount > 0 && (
                <span title={t`Працівників`}>
                    <Badge>{node.staffCount}</Badge>
                </span>
            )}
        </>
    );
}

/**
 * Keeps the nodes whose name, abbreviation or code contains the text, with the branches that lead to them. A matching
 * node keeps all its children.
 * @param nodes The nodes.
 * @param text The search text (already lower-cased and trimmed).
 * @returns The matching part of the tree.
 */
export function filterTree(nodes: readonly UnitTreeNode[], text: string): UnitTreeNode[] {
    if (!text) {
        return [...nodes];
    }

    return nodes.flatMap((node) => {
        const matches = [node.name, node.shortName, node.code].some((value) => value?.toLocaleLowerCase().includes(text));
        if (matches) {
            return [node];
        }

        const children = filterTree(node.children, text);
        return children.length > 0 ? [{ ...node, children }] : [];
    });
}
