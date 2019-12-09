import React from 'react';

interface Props {
    url: string;
    width?: number;
    height?: number;
}

export default React.memo((props: Props) => {
    const regex = new RegExp('https://((www\\.youtube\\.com)|(youtu\\.be))/(embed/)?(watch\\?v=)?(?<id>[^\\?&]*)[^\\?]*', 'gi');
    const result = regex.exec(props.url);
    if (!result || !result.groups || !result.groups["id"]) return null;

    const url = 'https://www.youtube.com/embed/' + result.groups["id"];
    return <iframe width={props.width || 560} height={props.height || 315} src={url} frameBorder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowFullScreen title="Flight Video"></iframe>
})