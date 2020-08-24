import React from 'react';
import styled from 'styled-components';
import Slider from 'react-slick';
import SimpleLightBox from "simple-lightbox";
import "simple-lightbox/dist/simpleLightbox.css";
import { AircraftData } from '../services/Models';

interface AircraftCardsProps {
    aircrafts: AircraftData[];
}

const properties = {
    dots: false,
    fade: true,
    infinite: true,
    speed: 800,
    slidesToShow: 1,
    slidesToScroll: 1,
    autoplay: true,
    autoplaySpeed: 3000,
    lazyLoad: true,
    arrows: false
}

export default (props: AircraftCardsProps) => {
    let index = 0;
    return <StyledList>
        {props.aircrafts.map(aircraft => {
            index++;
            return <StyledItem key={aircraft.tailNumber}>
                {aircraft.pictureUrls && aircraft.pictureUrls.length ?
                    <Slider {...properties} autoplaySpeed={3000 + (index % 5) * 200}>
                        {aircraft.pictureUrls.map(pictureUrl => (
                            <div key={pictureUrl}>
                                <img src={pictureUrl} alt={aircraft.title + " photo"} onClick={() => SimpleLightBox.open({
                                    items: aircraft.pictureUrls,
                                    startAt: aircraft.pictureUrls ? aircraft.pictureUrls.findIndex(u => u === pictureUrl) : 0
                                })} />
                            </div>
                        ))}
                    </Slider> :
                    <img src='aircraft_placeholder.png' alt={'No photo of ' + aircraft.title} />
                }

                <h4>{aircraft.title}</h4>
            </StyledItem>
        })}
    </StyledList>
}

const StyledList = styled.ul`
list-style: none;
padding: 0;
`

const StyledItem = styled.li`
height: 400px;
width: 100%;
max-width: 754px;
float: left;
border: 1px solid lightgray;
margin-right: 10px;
margin-bottom: 10px;
position: relative;

h4 {
background-color: rgba(255, 255, 255, 100);
position: absolute;
bottom: 0;
left: 0;
right: 0;
margin: 0;
padding: 5px;
}

img {
width: 100%;
}
`