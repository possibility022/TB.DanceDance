import * as React from 'react';
import { useState } from 'react';

export interface IDropdownProps {
    items: Array<string>
    selectedItemIndex: number
    startWithUnselected: boolean
    unselectedText?: string
    isLoading: boolean
    onSelected?: (selectedItem: string) => void
}

export function Dropdown(props: IDropdownProps) {

    const [selectedItem, setSelectedItem] = useState(props.startWithUnselected ? props.unselectedText : props.items[props.selectedItemIndex]);

    const dropdownDiv = React.createRef<HTMLDivElement>()

    const setSelected = (e: React.MouseEvent<HTMLDivElement, MouseEvent>) => {
        const key = e.currentTarget.getAttribute('data-key') as string;
        const index = Number.parseInt(key)
        setSelectedItem(props.items[index])
        dropdownDiv.current?.classList.remove("is-active")
    }

    const toogleDropdown = () => {
        dropdownDiv.current?.classList.toggle("is-active")
    }

    const getList = () => {
        let i = 0
        return props.items.map(r =>
            // todo, not sure if roleitem is correct.
            <div className="dropdown-item" role="menuitem" data-key={i} key={i++} onClick={(e) => setSelected(e)} >
                {r}
            </div>
        )
    }

    return (
        <div ref={dropdownDiv} className="dropdown">
            <div className="dropdown-trigger">
                <button className={props.isLoading ? "button is-loading" : "button"} aria-haspopup="true" aria-controls="dropdown-menu" onClick={() => toogleDropdown()} >
                    <span>{selectedItem}</span>
                    <span className="icon is-small">
                        <i className="fas fa-angle-down" aria-hidden="true"></i>
                    </span>
                </button>
            </div>
            <div className="dropdown-menu" id="dropdown-menu" role="menu">
                <div className="dropdown-content">
                    {getList()}
                </div>
            </div>
        </div>
    );
}
