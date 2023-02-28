import * as React from 'react';

export interface IButtonProps {
    onClick?(e: React.MouseEvent<HTMLButtonElement, MouseEvent>): void
    classNames?: string
    disabled?: boolean
}

export function Button(props: React.PropsWithChildren<IButtonProps>) {
    return (
        <button
            disabled={props.disabled === true}
            className={props.classNames ? "button " + props.classNames : "button"}
            onClick={(e) => { if (props.onClick) props.onClick(e) }}> {props.children}
        </button>
    );
}
