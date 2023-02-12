import * as React from 'react';

export interface IButtonProps {
    onClick?(e: React.MouseEvent<HTMLButtonElement, MouseEvent>): void
}

export function Button(props: React.PropsWithChildren<IButtonProps>) {
    return (
        <button className="button"  onClick={(e) => { if (props.onClick) props.onClick(e) }}> {props.children} </button>
    );
}
