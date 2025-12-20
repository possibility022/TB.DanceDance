import {format} from "date-fns";
import {pl} from "date-fns/locale";

const formatDateToPlDate = (date: Date) => {
    // don't ask me
    // todo: find out why I need to cast it to string
    const d = Date.parse(date.toLocaleString())
    return format(d, 'dd MMMM yyyy', { locale: pl })
}

const formatDateToYearOnly = (date: Date) => {
    // don't ask me
    // todo: find out why I need to cast it to string
    const d = Date.parse(date.toLocaleString())
    return format(d, 'yyyy', { locale: pl })
}

export {formatDateToPlDate, formatDateToYearOnly}