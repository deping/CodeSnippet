interface EnumType {
    [index: string]: number | string;
}

function makeEnum(arr: Array<string>): EnumType {
    const retVal: EnumType = {};
    arr.forEach((val, index) => {
        retVal[index] = val;
        retVal[val] = index;
    });
    Object.freeze(retVal);
    return retVal;
}
