import { SelectableButton } from '../SelectableButton';

export interface IItemToSelect<T extends number | string> {
    text: string
    selectionDisabled?: boolean
    key: T
}

export interface ISelectableListProps<T extends number | string> {
    header: string
    text: string
    options: Array<IItemToSelect<T>>
    onItemStatusChange(item: IItemToSelect<T>, isSelected: boolean): void
    articleClassName: string
}

export function SelectableList<T extends number | string>(props: ISelectableListProps<T>) {


    return (
        <div className="tile is-parent">
            <article className={"tile is-child notification  has-text-centered " + props.articleClassName}>
                <p className="title">{props.header}</p>
                <p>{props.text}</p>
                {props.options.map((v, i) => {
                    return (
                        <div className='field is-medium is-centered' key={v.key}>
                            <SelectableButton isSelected={false} isDisabled={v.selectionDisabled} onClick={(e, isSelected) => props.onItemStatusChange(v, isSelected)}>
                                {v.text}
                            </SelectableButton>
                        </div>)
                })}
            </article>
        </div>

    );
}
