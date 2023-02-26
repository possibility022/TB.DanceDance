import * as React from 'react';
import { useState } from 'react';

export interface ISelectableButtonProps {
    onClick?(e: React.MouseEvent<HTMLButtonElement, MouseEvent>): void
    isSelected: boolean
}

export function SelectableButton(props: React.PropsWithChildren<ISelectableButtonProps>) {

    const [isSelected, setIsSelected] = useState(props.isSelected)

    const onClick = (e: React.MouseEvent<HTMLButtonElement, MouseEvent>) => {
        if (props.onClick)
            props.onClick(e)

        setIsSelected(!isSelected)
    }

    return (
        <button className={isSelected ? "button is-primary" : "button"}  onClick={onClick}> {props.children} </button>
    );
}
