import * as React from 'react';

export interface IButtonProps {
    onClick?(e: React.MouseEvent<HTMLButtonElement, MouseEvent>): void
    content: JSX.Element
}

export function Button(props: IButtonProps) {
    return (
        <button className="button" onClick={(e) => { if (props.onClick) props.onClick(e) }}> {props.content} </button>
    );
}
