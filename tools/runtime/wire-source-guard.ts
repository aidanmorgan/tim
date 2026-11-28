import assert from 'node:assert/strict';
import { realpathSync } from 'node:fs';
import { createRequire } from 'node:module';
import { join } from 'node:path';
import type { Expression, Node } from 'typescript';

const root = realpathSync(process.cwd());
const require = createRequire(join(root, 'package.json'));
const compiler = require.resolve('typescript');
assert.equal(compiler, join(root, 'node_modules/typescript/lib/typescript.js'));
assert.equal(realpathSync(compiler), compiler);
const ts: typeof import('typescript') = require(compiler);

function unwrap(expression: Expression): Expression {
    while (ts.isParenthesizedExpression(expression) || ts.isAsExpression(expression) ||
        ts.isTypeAssertionExpression(expression) || ts.isNonNullExpression(expression) ||
        ts.isSatisfiesExpression(expression) || ts.isExpressionWithTypeArguments(expression)) {
        expression = expression.expression;
    }
    return expression;
}

function isIntegerLiteral(expression: Expression): boolean {
    const literal = unwrap(expression);
    if (ts.isPrefixUnaryExpression(literal) &&
        (literal.operator === ts.SyntaxKind.PlusToken || literal.operator === ts.SyntaxKind.MinusToken)) {
        return isIntegerLiteral(literal.operand);
    }
    return ts.isNumericLiteral(literal) && Number.isInteger(Number(literal.text));
}

function literalName(expression: Expression) {
    const literal = unwrap(expression);
    return ts.isStringLiteralLike(literal) ? literal : undefined;
}

function staticName(expression: Expression) {
    expression = unwrap(expression);
    if (ts.isElementAccessExpression(expression)) return literalName(expression.argumentExpression);
    const name = ts.isPropertyAccessExpression(expression) ? expression.name : expression;
    return ts.isIdentifier(name) ? name : undefined;
}

function frameName(node: Node) {
    if (ts.isIdentifier(node)) return node;
    if (ts.isElementAccessExpression(node)) return literalName(node.argumentExpression);
    if (ts.isComputedPropertyName(node)) return literalName(node.expression);
    if (!ts.isStringLiteralLike(node)) return undefined;

    // Computed and element keys are handled at their containing node.
    const parent = node.parent;
    if ((ts.isBindingElement(parent) && parent.propertyName === node) ||
        (ts.isPropertyAssignment(parent) && parent.name === node) ||
        ((ts.isImportSpecifier(parent) || ts.isExportSpecifier(parent)) && (parent.propertyName === node || parent.name === node))) {
        return node;
    }
    return undefined;
}

function scan(path: string, text: string, frameAdapter: boolean) {
    const source = ts.createSourceFile(path, text, ts.ScriptTarget.Latest, true);
    const violations: { path: string; line: number; reason: string }[] = [];
    function report(node: Node, reason: string): void {
        const { line } = source.getLineAndCharacterOfPosition(node.getStart(source));
        violations.push({ path, line: line + 1, reason });
    }
    function visit(node: Node): void {
        if (!frameAdapter) {
            const reference = frameName(node);
            if (reference?.text === '__has_identifier' || reference?.text === 'SIZE_PREFIX_LENGTH') {
                report(reference, 'frame check outside the shared frame adapter');
            }
        }
        if (ts.isCallExpression(node)) {
            const target = staticName(node.expression);
            if (target) {
                const name = target.text;
                const slot = name === '__offset' ? node.arguments[1]
                    : name === 'startObject' || name.startsWith('addField') ? node.arguments[0] : undefined;
                if (slot && isIntegerLiteral(slot)) report(target, 'literal FlatBuffers table slot or size');
            }
        }
        ts.forEachChild(node, visit);
    }
    visit(source);
    return violations;
}

let text = '';
for await (const chunk of process.stdin) text += String(chunk);
const request: unknown = JSON.parse(text);
assert.ok(Array.isArray(request));
const results = request.flatMap((item: unknown) => {
    assert.ok(item && typeof item === 'object' && 'path' in item && 'text' in item && 'frameAdapter' in item);
    assert.ok(typeof item.path === 'string' && typeof item.text === 'string' && typeof item.frameAdapter === 'boolean');
    return scan(item.path, item.text, item.frameAdapter);
});
process.stdout.write(JSON.stringify(results));
