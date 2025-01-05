import * as React from 'react';

export interface IApproveAndRejectButtonsProps<T> {
    onApprove: (arg: React.MouseEvent<HTMLButtonElement, MouseEvent>, input: T) => void
    onReject: (arg: React.MouseEvent<HTMLButtonElement, MouseEvent>, input: T) => void
    input: T
}

export function ApproveAndRejectButtons<T>(props: IApproveAndRejectButtonsProps<T>) {

    const [isEnabled, setIsEnabled] = React.useState(true)

    const onClick = (arg: React.MouseEvent<HTMLButtonElement, MouseEvent>, input: T, approved: boolean) => {
        setIsEnabled(false)

        if (approved)
            props.onApprove(arg, input)
        else
            props.onReject(arg, input)
    }

    return (
        <React.Fragment>
            <button disabled={!isEnabled} className="button is-success" onClick={(e) => onClick(e, props.input, true)}>Zatwierdź</button>
            <button disabled={!isEnabled} className="button is-danger ml-1" onClick={(e) => onClick(e, props.input, false)}>Nope!</button>
        </React.Fragment>
    );
}
