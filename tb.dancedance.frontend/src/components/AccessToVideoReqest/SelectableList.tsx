import * as React from 'react';
import { SelectableButton } from '../SelectableButton';

export interface IItemToSelect {
    text: string
    key: React.Key
}

export interface ISelectableListProps {
    header: string
    text: string
    options: Array<IItemToSelect>
    onItemSelected(item: IItemToSelect): void
    articleClassName: string
}

export function SelectableList(props: ISelectableListProps) {


    return (
        <div className="tile is-parent">
            <article className={"tile is-child notification  has-text-centered " + props.articleClassName}>
                <p className="title">{props.header}</p>
                <p>{props.text}</p>
                {props.options.map((v, i) => {
                    return (
                        <div className='field is-medium is-centered' key={v.key}>
                            <SelectableButton isSelected={false}>
                                {v.text}
                            </SelectableButton>
                        </div>)
                })}
            </article>
        </div>

    );
}
