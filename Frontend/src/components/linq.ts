export function* take<T>(source: Iterable<T>, count: number): Iterable<T> {
    let i = 0;
    for (const item of source) {
        if (i >= count) {
            break;
        }
        yield item;
        i++;
    }
}

export function* skip<T>(source: Iterable<T>, count: number): Iterable<T> {
    let i = 0;
    for (const item of source) {
        if (i >= count) {
            yield item;
        }
        i++;
    }
}

export function* where<T>(source: Iterable<T>, predicate: (item: T) => boolean): Iterable<T> {
    for (const item of source) {
        if (predicate(item)) {
            yield item;
        }
    }
}

export function* select<T, U>(source: Iterable<T>, selector: (item: T) => U): Iterable<U> {
    for (const item of source) {
        yield selector(item);
    }
}

export function* selectMany<T, U>(source: Iterable<T>, selector: (item: T) => Iterable<U>): Iterable<U> {
    for (const item of source) {
        yield* selector(item);
    }
}

export function* concat<T>(source: Iterable<T>, other: Iterable<T>): Iterable<T> {
    yield* source;
    yield* other;
}

export function* distinct<T>(source: Iterable<T>): Iterable<T> {
    const set = new Set<T>();
    for (const item of source) {
        if (!set.has(item)) {
            set.add(item);
            yield item;
        }
    }
}

export function* distinctBy<T>(source: Iterable<T>, predicate: (item: T) => boolean): Iterable<T> {
    const set = new Set<T>();
    for (const item of source) {
        if (!set.has(item)) {
            set.add(item);
            yield item;
        }
    }
}

export function* chunk<T>(source: Iterable<T>, count: number): Iterable<T[]> {
    let chunk: T[] = [];
    for (const item of source) {
        chunk.push(item);
        if (chunk.length >= count) {
            yield chunk;
            chunk = [];
        }
    }
    if (chunk.length > 0) {
        yield chunk;
    }
}

export function* map<T, U>(source: Iterable<T>, selector: (item: T) => U): Iterable<U> {
    for (const item of source) {
        yield selector(item);
    }
}