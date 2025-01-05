import * as React from 'react';

export interface IButtonProps {
    onClick?(e: React.MouseEvent<HTMLButtonElement, MouseEvent>): void
    classNames?: string
    disabled?: boolean
    isLoading?: boolean
}

export function Button(props: React.PropsWithChildren<IButtonProps>) {

    const getClasses = () => {
        let classNames = "button";

        if (props.classNames)
            classNames += " " + props.classNames

        if (props.isLoading)
            classNames += " " + "is-loading"

        return classNames
    }

    return (
        <button
            disabled={props.disabled === true}
            className={getClasses()}
            onClick={(e) => { if (props.onClick) props.onClick(e) }}> {props.children}
        </button>
    );
}
