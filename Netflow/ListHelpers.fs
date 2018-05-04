module ListHelpers

let combine2 xs ys = [
    for x in xs do
    for y in ys do
    yield [x; y] ]

let combine3 xs ys zs = [
    for x in xs do
    for y in ys do
    for z in zs do
    yield [x; y; z] ]